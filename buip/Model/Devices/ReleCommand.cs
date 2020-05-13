using buip.Shared;
using System;

namespace buip.Model.Devices
{
    public class ReleCommand<T> : DeviceCommand<T> where T : struct, Enum
    {
        public ReleCommand() : base()
        {
        }

        public ReleCommand(byte[] data) : base()
        {
            this.data = data;
        }
        public ReleCommand(int number, bool isOn) : this(new byte[] { (byte)((number - 1) / 8 + 1), (byte)((number - 1) % 8), (byte)(isOn ? 1 : 0) }) { }

        public ReleCommand(byte addr, byte mask) : this(new byte[] { addr, mask }) { }

        public override byte[] ToArray() => data;
    }
}
