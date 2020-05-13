using buip.Shared;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace buip.Exchangers
{
    public abstract class Exchanger<T> : IExchangeable<T> where T : struct, Enum
    {
        public event EventHandler<TEventArgs<DeviceCommand<T>>> OnDataReceive;
        public event EventHandler<TEventArgs<DeviceCommand<T>>> OnError;

        private CancellationTokenSource source;

        private TaskCompletionSource<bool> tcs;

        private bool isStarted;

        protected readonly static object lockObject = new object();
        public List<DeviceCommand<T>> Commands { get; } = new List<DeviceCommand<T>>();
        public DeviceCommand<T> BaseCommand { get; set; }

        public abstract void OnListen();
        public abstract void OnRequest();
        public abstract void OnStop();


        public void StartListen()
        {
            if (isStarted) return;
            source = new CancellationTokenSource();
            var token = source.Token;
            Task.Run(async () =>
            {
                isStarted = true;
                while (!token.IsCancellationRequested)
                {
                    OnListen();
                    await Task.Delay(15);
                }
                Console.WriteLine("StartListen finish");
                OnStop();
                tcs.SetResult(true);
            }, token);
        }

        public void StartRequest()
        {
            if (isStarted) return;
            source = new CancellationTokenSource();
            var token = source.Token;
            Task.Run(async () =>
            {
                isStarted = true;

                while (!token.IsCancellationRequested)
                {
                    
                    try
                    {
                        OnRequest();
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"{ex.Message} : {ex.StackTrace}");
                    }
                    
                    await Task.Delay(15);
                }
                Console.WriteLine("StartRequest finish");
                OnStop();
                tcs.SetResult(false);
            }, token);
        }
        protected void ErrorEvent(DeviceCommand<T> cmd) => OnError?.Invoke(this, new TEventArgs<DeviceCommand<T>>(cmd));
        protected void DataReceiveEvent(DeviceCommand<T> cmd) => OnDataReceive?.Invoke(this, new TEventArgs<DeviceCommand<T>>(cmd));
        public async Task StopAsync()
        {
            if (isStarted)
            {
                Console.WriteLine("StopAsync");
                tcs = new TaskCompletionSource<bool>();
                source.Cancel();
                isStarted = await tcs.Task;
            }
        }

        public void AddCommand(DeviceCommand<T> cmd)
        {
            lock (lockObject)
            {
                Commands.Add(cmd);
            }
        }

        public void RemoveCommand(DeviceCommand<T> cmd)
        {
            if (Commands.Contains(cmd))
            {
                lock (lockObject)
                {
                    Commands.Remove(cmd);
                }
            }
            cmd.Complete();
        }
    }


}
