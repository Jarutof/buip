using buip.Shared;
using System;
using System.Collections.Generic;

namespace buip.Exchangers
{
    public class StabiliserExchanger : Exchanger<BoardStatusEnum>
    {
        public const int AnswerTimeOut = 200;
        private readonly ISA_Port port;

        private bool WriteDataHi(byte data)
        {
            port.WriteWithOffset(data, 1);
            DateTime dt = DateTime.Now;
            while ((port.ReadWithOffset(1) & 0x03) != (data & 0x03))
            {
                if ((DateTime.Now - dt).TotalMilliseconds > AnswerTimeOut) return false;
            }
            return true;
        }
        private bool WriteDataLo(byte data)
        {
            port.Write(data);
            DateTime dt = DateTime.Now;
            while (port.Read() != data)
            {
                if ((DateTime.Now - dt).TotalMilliseconds > AnswerTimeOut) return false;
            }
            return true;
        }


        public override void OnListen()
        {

        }

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

                if (!WriteDataLo(cmd.ToArray()[0]))
                {
                    cmd.Data.Set(new byte[] { cmd.ToArray()[0] }, BoardStatusEnum.BasePortError);
                    ErrorEvent(cmd);
                    return;
                }
                if (!WriteDataHi(cmd.ToArray()[1]))
                {
                    cmd.Data.Set(new byte[] { cmd.ToArray()[1] }, BoardStatusEnum.OffsetPortError);
                    ErrorEvent(cmd);
                    return;
                }
                byte lo = port.Read();
                byte hi = port.ReadWithOffset(1);


                List<byte> buffer = new List<byte>(new byte[] { lo, hi });
                if (cmd.MustBeEqualToRequest && !buffer.DeepEqual(cmd.ToArray()))
                {
                    cmd.Data.Set(buffer, BoardStatusEnum.DataFail);
                    ErrorEvent(cmd);
                    return;
                }

                cmd.Data.Set(buffer, BoardStatusEnum.Ok);
                RemoveCommand(cmd);
            }
            else
            {
                List<byte> buffer = new List<byte>();
                buffer.Add(port.Read());
                buffer.Add(port.ReadWithOffset(1));

                BaseCommand.Data.Set(buffer, BoardStatusEnum.Ok);
                DataReceiveEvent(BaseCommand);
            }

        }

        public override void OnStop()
        {
            port.Close();
        }

        public StabiliserExchanger(UInt16 portName)
        {
            port = new ISA_Port(portName, 2);
        }
    }
}
