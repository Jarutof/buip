using System;
using System.Collections.Generic;

namespace buip.Shared
{
    public class DeviceData<T> where T : struct, Enum
    {
        public T Status { get; private set; }
        public List<byte> Buffer { get; private set; }

        public void Set(IEnumerable<byte> buffer, T status)
        {
            Status = status;
            Buffer = new List<byte>(buffer);
        }
    }
}
