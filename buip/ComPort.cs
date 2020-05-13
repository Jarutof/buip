using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Threading;

namespace buip
{
    public class ComPort
    {
        //Перечисление статусов отправляемой комманды
        public enum StatusEnum
        {
            None, Ok, ConfirmTimeout, ConfirmFail, NoAnswer, AnswerTimeout, CRCFail, Process, OpenPortFail, DecodeFail,
            HiBitFail,
            DataFail
        }

        private SerialPort port;
        public ComProtocol Protocol { get; private set; } = ComProtocol.Default;
        public string PortName { get; private set; }

        internal void Flush()
        {
            if (!OpenPort()) return;
            while (port.BytesToRead > 0) port.ReadByte();
        }

        public List<byte> Buffer { get; private set; } = new List<byte>();
        public StatusEnum Status { get; private set; } = StatusEnum.None;

        public ComPort(string portName, ComProtocol protocol)
        {
            PortName = portName;
            Protocol = protocol;
            port = new SerialPort(PortName, Protocol.BoudRate, Parity.None, 8, StopBits.One);
        }

        public void Close()
        {
            Console.WriteLine($"Port Close {PortName}");
            port.Close();
        }

        public bool OpenPort()
        {
            if (!port.IsOpen)
            {
                try
                {
                    port.Handshake = Handshake.None;
                    port.Open();
                }
                catch
                {
                    Console.WriteLine("OpenPort fail");
                    return false;
                }
            }

            if (Protocol.WithRTS)
            {
                port.RtsEnable = true;
            }

            return true;
        }
        public StatusEnum Send(IEnumerable<byte> data)
        {
            if (!OpenPort()) return StatusEnum.OpenPortFail;
            port.Write(data.ToArray(), 0, data.Count());
            return StatusEnum.Ok;
        }
        public void SendSafely(IEnumerable<byte> data)
        {
            byte[] tdata = Encode(data.ToList(), Protocol.POLY);

            port.Write(new byte[1] { (byte)(Protocol.STX + Protocol.Address) }, 0, 1);
            port.Write(tdata, 0, tdata.Length);
            port.Write(new byte[1] { (byte)(Protocol.ETX + Protocol.Address) }, 0, 1);
        }

        private void Write(byte data)
        {
            port.Write(new byte[1] { data }, 0, 1);
        }
        public StatusEnum Receive(int receiveCount)
        {

            if (!OpenPort()) return StatusEnum.OpenPortFail;
            Buffer.Clear();
            DateTime timerStart = DateTime.Now;
            while ((DateTime.Now - timerStart).TotalMilliseconds < Protocol.WaitForAnswer)
            {
                if (port.BytesToRead > 0)
                {
                    byte data = (byte)port.ReadByte();
                    Buffer.Add(data);
                    if (Buffer.Count == receiveCount) return StatusEnum.Ok;
                }
                Thread.Sleep(15);
            }
            return StatusEnum.ConfirmTimeout;
        }

        public StatusEnum ReceiveSafely()
        {
            if (!OpenPort()) return StatusEnum.OpenPortFail;

            DateTime timerStart = DateTime.Now;
            while ((DateTime.Now - timerStart).TotalMilliseconds < Protocol.WaitForAnswer)
            {
                int bytesToRead = port.BytesToRead;
                if (bytesToRead > 0)
                {
                    byte[] buffer = new byte[bytesToRead];
                    port.Read(buffer, 0, bytesToRead);

                    foreach (byte data in buffer)
                    {
                        //Проверка на старший бит
                        if (data.IsBit(7))
                        {
                            if (data == (Protocol.STX + Protocol.Address))
                            {
                                Write(data);
                                timerStart = DateTime.Now;
                                Buffer.Clear();
                            }
                            else
                            if (data == (Protocol.ETX + Protocol.Address))
                            {
                                try
                                {

                                    Buffer = Decode(Buffer);

                                    if (CheckCRC(Buffer, Protocol.POLY))
                                    {
                                        return StatusEnum.Ok;
                                    }
                                    else
                                    {
                                        return StatusEnum.CRCFail;
                                    }

                                }
                                catch
                                {
                                    return StatusEnum.DecodeFail;
                                }
                            }
                            else
                            {
                                return StatusEnum.HiBitFail;
                            }
                        }
                        else
                        {
                            Buffer.Add(data);
                        }
                    }
                }
                Thread.Sleep(15);
            }
            return StatusEnum.ConfirmTimeout;
        }
        public static bool CheckCRC(List<byte> data, byte poly)
        {
            byte crc = data[data.Count - 1];
            List<byte> part = data.GetRange(0, data.Count - 1);
            return (crc == MeasureCRC(part, poly));
        }
        //Вычисление контрольной суммы
        public static byte MeasureCRC(List<byte> data, byte poly)
        {
            byte crc = 0x00;
            int len = data.Count;
            ushort j = 0;

            while (len-- > 0)
            {
                crc ^= data[j++];
                for (int i = 0; i < 8; i++)
                    crc = ((crc & 0x01) != 0) ? (byte)((crc >> 1) ^ poly) : (byte)(crc >> 1);
            }
            return crc;
        }

        //Кодирование данных для отправки
        public static byte[] Encode(List<byte> list, byte poly)
        {
            int count = (list.Count + 2 + list.Count / 7);
            byte[] result = new byte[count];

            for (int i = 0; i < list.Count + 1; i++)
            {
                result[i] = (byte)(i < list.Count ? list[i] : MeasureCRC(list, poly));
                result[list.Count + 1 + i / 7] = (byte)((byte)(result[i] & 0x80) == 0x80 ? result[list.Count + 1 + i / 7] | (1 << i % 7) : result[list.Count + 1 + i / 7] & ((1 << i % 7) ^ 0xFF));
                result[i] &= 0x7F;
            }
            return result;
        }

        public static List<byte> Decode(List<byte> data)
        {
            List<byte> result = new List<byte>();
            int n;

            for (int i = 0; i < data.Count; i++)
            {
                n = (int)Math.Ceiling((double)i / 7);
                if (data.Count - i == n) break;
                else result.Add(data[i]);
            }

            for (int i = 0; i < result.Count; i++)
                if ((data[result.Count + (int)Math.Floor((double)(i / 7))] & (byte)(1 << (i % 7))) != 0) result[i] |= 0x80;

            return result;
        }

        public struct ComProtocol
        {
            public byte STX;
            public byte ETX;
            public byte POLY;
            public int WaitForAnswer;
            public bool WithRTS;
            public int BoudRate;
            public byte Address;

            public static ComProtocol Default
            {
                get
                {
                    return new ComProtocol
                    {
                        STX = 0x80,
                        ETX = 0xD0,
                        POLY = 0x8C,
                        WaitForAnswer = 1000,
                        BoudRate = 19200,
                        Address = 1
                    };
                }
            }
        }
    }
}
