using System;
using System.Runtime.InteropServices;

namespace buip
{
    public class ISA_Port : IDisposable
    {
        [DllImport("isa_driver.so", EntryPoint = "port_open")]
        private static extern int Port_open(ushort port, ushort num);

        [DllImport("isa_driver.so", EntryPoint = "port_close")]
        private static extern int Port_close(ushort port, ushort num);

        [DllImport("isa_driver.so", EntryPoint = "port_read")]
        private static extern byte Port_read(ushort port);

        [DllImport("isa_driver.so", EntryPoint = "port_write")]
        private static extern void Port_write(ushort port, byte val);

        [DllImport("isa_driver.so", EntryPoint = "port_write_array")]
        private static extern void Port_write(ushort port, byte[] val, UInt16 length);


        public UInt16 BaseAddress { get; private set; }
        public UInt16 Count { get; private set; }

        public bool IsOpen { get; private set; } = false;

        private ISA_Port(UInt16 baseAddress)
        {
            BaseAddress = baseAddress;
        }

        public ISA_Port(UInt16 baseAddress, UInt16 count) : this(baseAddress)
        {
            Count = count;
            Open();
        }

        public bool Open()
        {
            if (!IsOpen)
            {
                try
                {
                    IsOpen = Port_open(BaseAddress, Count) == 0;
                }
                catch
                {
                }
            }
            return IsOpen;
        }

        public virtual void Write(byte data)
        {
            Port_write(BaseAddress, data);
        }

        public virtual void WriteArray(byte[] data)
        {
            Port_write(BaseAddress, data, (UInt16)data.Length);
        }

        public void WriteWithOffset(byte data, int o)
        {
            Port_write((UInt16)(BaseAddress + o), data);
        }

        public byte ReadWithOffset(int o)
        {
            return Read((UInt16)(BaseAddress + o));
        }
        public byte Read(UInt16 addr)
        {
            return (byte)Port_read(addr);
        }
        public byte Read()
        {
            return (byte)Port_read(BaseAddress);
        }
        public void Close()
        {
            Console.WriteLine($"ISA_Port close {BaseAddress}");
            Port_close(BaseAddress, Count);
            IsOpen = false;
        }
        public void Dispose()
        {
            Close();
        }
    }
}
