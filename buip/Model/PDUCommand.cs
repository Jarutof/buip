using buip.Shared;
using System;

namespace buip.Model
{
    public class PDUCommand<T> : DeviceCommand<T> where T : struct, Enum
    {
        public override byte[] ToArray()
        {
            return new byte[] { 0 };
        }
    }
}
