using System;

namespace buip
{
    public class TEventArgs<T> : EventArgs
    {
        public T Value { get; private set; }
        public TEventArgs(T value)
        {
            Value = value;
        }
    }
}
