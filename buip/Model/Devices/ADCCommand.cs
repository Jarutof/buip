using buip.Shared;
using System;
using System.Linq;

namespace buip.Model.Devices
{
    public enum CommandsEnum
    {
        ReadInputChannels = 0x04,
        ModuleSettings = 0x46,
        Error = 0xC6
    }
    public class ADCCommand<T> : DeviceCommand<T> where T : struct, Enum
    {

        public override byte[] ToArray() => data;

        public static ADCCommand<T> GetMeasures { get => new ADCCommand<T>(new byte[] { 1, (byte)CommandsEnum.ReadInputChannels, 0, 0, 0, 0x0A/*, 0x70, 0x0D*/ }) { AnswerLength = 25 }; }
        public static ADCCommand<T> ConfigureChannels(UInt16 mask) => new ADCCommand<T>(new byte[] { 1, (byte)CommandsEnum.ModuleSettings, 0x26, mask.Hi(), mask.Lo() }) { AnswerLength = 6 };


        public ADCCommand(byte[] data, bool addCRC = true) : base(data)
        {
            this.data = addCRC ? data.AddCRC16().ToArray() : data;
        }

        public static ADCCommand<T> ConfigureChannel(byte bit, byte conf) => new ADCCommand<T>(new byte[] { 1, (byte)CommandsEnum.ModuleSettings, 0x8, 0, bit, conf }) { AnswerLength = 6 };
    }
}
