using buip.Shared;
using System;
using System.Collections.Generic;

namespace buip.Exchangers
{

    public class BkiExchanger : ComPortExchanger
    {
        public BkiExchanger(string portName, int boudrate) : base(portName, boudrate) { }
        public override void OnRequest()
        {
            port.Flush();
            ComPort.StatusEnum status;
            DeviceCommand<ComPort.StatusEnum> cmd = null;

            lock (lockObject)
            {
                if (Commands.Count > 0)
                    cmd = Commands[0];
            }

            if (cmd != null)
            {
                status = port.Send(cmd.ToArray());

                if (status != ComPort.StatusEnum.Ok)
                {
                    cmd.Data.Set(port.Buffer, status);
                    ErrorEvent(cmd);
                    return;
                }

                status = port.Receive(cmd.AnswerLength);

                if (status == ComPort.StatusEnum.Ok)
                {
                    List<byte> buffer = new List<byte>(port.Buffer);

                    if (cmd.MustBeEqualToRequest && !buffer.DeepEqual(cmd.ToArray()))
                    {
                        Console.WriteLine("DataFail");
                        cmd.Data.Set(port.Buffer, ComPort.StatusEnum.DataFail);
                        ErrorEvent(cmd);
                        return;
                    }

                    cmd.Data.Set(port.Buffer, status);
                    DataReceiveEvent(cmd);
                }
                else
                {
                    cmd.Data.Set(port.Buffer, status);
                    ErrorEvent(cmd);
                }
            }
            else
            {
                status = port.Send(BaseCommand.ToArray());

                if (status != ComPort.StatusEnum.Ok)
                {
                    BaseCommand.Data.Set(port.Buffer, status);
                    ErrorEvent(BaseCommand);
                    return;
                }
                status = port.Receive(BaseCommand.AnswerLength);

                if (status == ComPort.StatusEnum.Ok)
                {
                    BaseCommand.Data.Set(port.Buffer, status);
                    DataReceiveEvent(BaseCommand);

                }
                else
                {
                    BaseCommand.Data.Set(port.Buffer, status);
                    ErrorEvent(BaseCommand);
                }
            }
        }

    }
}
