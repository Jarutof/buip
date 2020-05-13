using buip.Shared;
using System;

namespace buip.Model.Devices
{
    public class StabilizerCommand<T> : DeviceCommand<T> where T : struct, Enum
    {
        public static StabilizerCommand<T> Check { get { return new StabilizerCommand<T>(new byte[] { 0, 0xCC }) { MustBeEqualToRequest = true }; } }

        public override byte[] ToArray() => data;

        public StabilizerCommand(byte[] data, int answLength = 2) : base(data, answLength) { }

        public StabilizerCommand() : base()
        {
        }

        public static StabilizerCommand<T> SetCode(ushort code) => new StabilizerCommand<T>(new byte[] { code.Lo(), code.Hi() });
    }
}
