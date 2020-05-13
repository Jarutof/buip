using buip.Shared;
using System;
using System.Collections.Generic;

namespace buip.Exchangers
{
    internal class ReleExchanger : Exchanger<BoardStatusEnum>
    {
        public const int AnswerTimeOut = 200;
        private readonly ISA_Port port;
        private object lockObj = new object();

        private bool WriteAddress(byte addr)
        {
            addr += 1;
            port.Write(addr);
            DateTime dt = DateTime.Now;
            while ((port.Read() & 0x7F) != addr)
            {
                if ((DateTime.Now - dt).TotalMilliseconds > AnswerTimeOut)
                {
                    return false;
                }
            }
            return true;
        }
        private bool WriteData(byte data)
        {

            port.WriteWithOffset(data, 1);
            DateTime dt = DateTime.Now;
            while ((port.ReadWithOffset(1) ^ 0xFF) != data)
            {
                if ((DateTime.Now - dt).TotalMilliseconds > AnswerTimeOut)
                {
                    Console.WriteLine($"rele {port.BaseAddress}: error write data {data} { port.ReadWithOffset(1) ^ 0xFF} ");
                    return false;
                }
            }
            return true;
        }
        private byte ReadData() => port.ReadWithOffset(1);
        public ReleExchanger(UInt16 portName)
        {
            port = new ISA_Port(portName, 2);
        }


        public override void OnListen() { }

        public override void OnRequest()
        {
            DeviceCommand<BoardStatusEnum> cmd = null;
            lock (lockObject)
            {
                if (Commands.Count > 0)
                    cmd = Commands[0];
            }

            if (cmd != null)
            {

                byte addr = cmd.ToArray()[0];

                if (!WriteAddress(addr))
                {
                    cmd.Data.Set(new byte[] { addr }, BoardStatusEnum.BasePortError);
                    ErrorEvent(cmd);
                    return;
                }

                byte data = (byte)(cmd.ToArray()[1] ^ 0xFF);
                if (!WriteData(data))
                {
                    cmd.Data.Set(new byte[] { data }, BoardStatusEnum.OffsetPortError);
                    ErrorEvent(cmd);
                    return;
                }

                cmd.Data.Set(cmd.ToArray(), BoardStatusEnum.Ok);
                RemoveCommand(cmd);
            }
            else
            {
                List<byte> buffer = new List<byte>();
                for (int i = 0; i < 4; i++)
                {
                    if (!WriteAddress((byte)i))
                    {
                        BaseCommand.Data.Set(new byte[] { (byte)(i + 1) }, BoardStatusEnum.BasePortError);
                        ErrorEvent(BaseCommand);
                        return;
                    }
                    buffer.Add(ReadData());
                }
                BaseCommand.Data.Set(buffer, BoardStatusEnum.Ok);
                DataReceiveEvent(BaseCommand);
            }
        }

        public override void OnStop() => port.Close();
    }
}