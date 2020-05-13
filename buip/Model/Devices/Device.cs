using buip.Shared;
using System;
using System.Threading.Tasks;

namespace buip.Model.Devices
{
    public abstract class Device
    {
        public abstract Task<bool> InitAsync();

        public abstract Task<bool> CheckAsync();
        public abstract void SetInitState();
    }
    public abstract class Device<T, TCommand> : Device where TCommand : DeviceCommand<T> where T : struct, Enum
    {
        IExchangeable<T> exchanger;
        public bool IsConnected { get; set; }

        public DeviceData<T> Data { get; set; }

        protected int attemptsMax = 3;
        protected int attempts;
        private bool isFirstExhange = true;

        public abstract void DataChangeHandler(ICommand<T> value);
        public abstract void ErrorHandler(ICommand<T> value);
        public override void SetInitState()
        {
            IsConnected = true;
        }
        public Device(IExchangeable<T> exchanger)
        {
            this.exchanger = exchanger;
            exchanger.OnDataReceive += (s, e) =>
            {
                IsConnected = true;
                attempts = 0;
                DataChangeHandler(e.Value);
                exchanger.RemoveCommand(e.Value);
            };

            exchanger.OnError += (s, e) =>
            {
                if (attempts == attemptsMax)
                {
                    if (IsConnected || isFirstExhange)
                    {
                        IsConnected = false;
                        isFirstExhange = false;
                        ErrorHandler(e.Value);
                    }
                    exchanger.RemoveCommand(e.Value);
                    attempts = 0;
                }
                else attempts++;

            };
        }
        public async Task<DeviceData<T>> SendCommandAsync(TCommand cmd)
        {
            if (exchanger.Commands.Count > 0)
            {
                Console.WriteLine("!!!!!!!!!!!!!!! OLOLO !!!!!!!!!!!!!!!");
            }

            cmd.CompletionSource = new TaskCompletionSource<DeviceData<T>>();
            exchanger.AddCommand(cmd);
            return await cmd.CompletionSource.Task;
        }
    }
}
