using System;
using System.Collections.Generic;
using System.Threading;

namespace buip
{


    public sealed class ISA_SerialPort : ISA_Port, IDisposable
    {
        public enum StatusEnum
        {
            Ok, ConfirmTimeOut,
            OpenPortFail,
        }

        public List<byte> Buffer { get; private set; } = new List<byte>();
        public PortProtocol Protocol { get; private set; }

        public ISA_SerialPort(UInt16 addr) : base(addr, 8) { }
        public ISA_SerialPort(UInt16 addr, Int32 boudrate) : this(addr)
        {
            SetBoudrate(boudrate);
            SetFifo();
            Init();
        }
        public ISA_SerialPort(UInt16 addr, PortProtocol protocol) : this(addr, protocol.BoudRate) => Protocol = protocol;

        private void Init()
        {
            WriteWithOffset(3, 3);
        }
        private void SetFifo()
        {
            WriteWithOffset(1, 2);
        }
        private void SetBoudrate(int boudrate)
        {
            byte data = ReadWithOffset(3);
            WriteWithOffset((byte)(data | 0x80), 3);
            base.Write((byte)(115200 / boudrate));
            data = ReadWithOffset(3);
            WriteWithOffset((byte)(data & 0x7F), 3);
        }
        public override void Write(byte data)
        {
            while (!ReadWithOffset(5).IsBit(6)) ;
            base.Write(data);
        }

        public override void WriteArray(byte[] data)
        {
            while (!ReadWithOffset(5).IsBit(6)) ;

            base.WriteArray(data);
        }

        private bool IsByteToRead()
        {
            return ReadWithOffset(5).IsBit(0);
        }

        public StatusEnum Write(IEnumerable<byte> requestCommand)
        {
            if (!Open()) return StatusEnum.OpenPortFail;

            WriteArray(new List<byte>(requestCommand).ToArray());

            return StatusEnum.Ok;
        }

        public StatusEnum Receive(int receiveCount)
        {
            Buffer.Clear();
            DateTime timerStart = DateTime.Now;
            while ((DateTime.Now - timerStart).TotalMilliseconds < Protocol.WaitForAnswer)
            {
                if (IsByteToRead())
                {
                    DateTime timerBytesRead = DateTime.Now;
                    while ((DateTime.Now - timerBytesRead).TotalMilliseconds < 30)
                    {
                        if (IsByteToRead())
                        {
                            Buffer.Add(Read());
                        }
                    }
                }
                if (Buffer.Count == receiveCount)
                {
                    return StatusEnum.Ok;
                }

                Thread.Sleep(1);
            }

            return StatusEnum.ConfirmTimeOut;
        }

        internal void Flush()
        {
            if (!Open()) return;
            while (IsByteToRead()) Read();
        }
    }
}
