using System;
using System.Threading.Tasks;

namespace buip.Shared
{
    public interface ICommand<T> where T : struct, Enum
    {
        DeviceData<T> Data { get; set; }
        TaskCompletionSource<DeviceData<T>> CompletionSource { get; set; }
        byte[] ToArray();
        int AnswerLength { get; set; }
        bool MustBeEqualToRequest { get; set; }

        void Complete();
    }
}
