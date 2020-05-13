using buip.Shared;
using System;
using System.Collections.Generic;

namespace buip
{
    public interface IExchangeable<T> where T : struct, Enum
    {
        event EventHandler<TEventArgs<DeviceCommand<T>>> OnDataReceive;
        event EventHandler<TEventArgs<DeviceCommand<T>>> OnError;
        void RemoveCommand(DeviceCommand<T> cmd);
        void AddCommand(DeviceCommand<T> cmd);
        List<DeviceCommand<T>> Commands { get; }
    }
}
