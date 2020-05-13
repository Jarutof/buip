using buip.Shared;
using System;

namespace buip.Exchangers
{
    public class AdcExchanger : Exchanger<ISA_SerialPort.StatusEnum>
    {
        private readonly ISA_SerialPort port;
        public UInt16 Name { get; private set; }
        public AdcExchanger(UInt16 portName, int boudrate)
        {
            Name = portName;
            PortProtocol protocol = PortProtocol.Default;
            protocol.BoudRate = boudrate;
            port = new ISA_SerialPort(portName, protocol);
        }

        public override void OnListen()
        {

        }

        public override void OnRequest()
        {
            port.Flush();
            DeviceCommand<ISA_SerialPort.StatusEnum> cmd;
            lock (lockObject)
            {
                if (Commands.Count > 0)
                    cmd = Commands[0];
                else
                    cmd = BaseCommand;
            }

            ISA_SerialPort.StatusEnum status = port.Write(cmd.ToArray());
            if (status != ISA_SerialPort.StatusEnum.Ok)
            {
                cmd.Data.Set(port.Buffer, status);
                ErrorEvent(cmd);
                return;
            }
            status = port.Receive(cmd.AnswerLength);
            if (status == ISA_SerialPort.StatusEnum.Ok)
            {
                cmd.Data.Set(port.Buffer, status);
                DataReceiveEvent(cmd);
            }
            else
            {
                cmd.Data.Set(port.Buffer, status);
                ErrorEvent(cmd);
            }

        }
       
        public override void OnStop() => port.Close();
    }
}
