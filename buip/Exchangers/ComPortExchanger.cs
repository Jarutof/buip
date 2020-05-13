using System.Collections.Generic;

namespace buip.Exchangers
{
    public class ComPortExchanger : Exchanger<ComPort.StatusEnum>
    {
        protected ComPort port;
        public ComPortExchanger(string portName, int boudrate)
        {
            ComPort.ComProtocol protocol = ComPort.ComProtocol.Default;
            protocol.BoudRate = boudrate;
            port = new ComPort(portName, protocol);
        }

        public void Answer(IEnumerable<byte> data)
        {
            port.SendSafely(data);
        }

        public override void OnListen()
        {
            var status = port.ReceiveSafely();
            switch (status)
            {
                case ComPort.StatusEnum.Ok:
                    BaseCommand?.Data.Set(port.Buffer, status);
                    DataReceiveEvent(BaseCommand);
                    break;
                default:
                    BaseCommand?.Data.Set(port.Buffer, status);
                    ErrorEvent(BaseCommand);
                    break;
            }
        }

        public override void OnRequest() { }

        public override void OnStop() => port.Close();
    }
}
