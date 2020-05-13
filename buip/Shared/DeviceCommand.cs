using System;
using System.Threading.Tasks;

namespace buip.Shared
{
    public abstract class DeviceCommand<T> : ICommand<T> where T : struct, Enum
    {
        protected byte[] data;

        public DeviceData<T> Data { get; set; }
        public TaskCompletionSource<DeviceData<T>> CompletionSource { get; set; }
        public int AnswerLength { get; set; }
        public bool MustBeEqualToRequest { get; set; }

        public void Complete()
        {
            CompletionSource?.SetResult(Data);
        }

        public abstract byte[] ToArray();
        public DeviceCommand()
        {
            Data = new DeviceData<T>();
        }

        public DeviceCommand(byte[] data) : this()
        {
            this.data = data;
        }

        public DeviceCommand(byte[] data, int answLength) : this(data)
        {
            AnswerLength = answLength;
        }
    }
}
