using buip.Shared;
using System;

namespace buip.Model.Devices
{
    public class BKICommand<T> : DeviceCommand<T> where T : struct, Enum
    {
        public enum Command
        {
            Status = 0x60,
            Commut = 0x40,
            MeasureR = 0x80,
            MeasureV = 0xE0,
            Uncomm = 0x20,
            Voltage = 0xA0,
            RegWrite = 0xC2,
            RegRead = 0xC0,

        }
        public static BKICommand<T> Init { get { return new BKICommand<T>(new byte[] { (byte)Command.Uncomm, 0, 0 }) { MustBeEqualToRequest = true }; } }
        public static BKICommand<T> Status { get { return new BKICommand<T>(new byte[] { (byte)Command.Status, 0, 0 }, answLength: 10); } }
        public static BKICommand<T> Uncommitation { get { return new BKICommand<T>(new byte[] { (byte)Command.Uncomm, 0, 0 }) { MustBeEqualToRequest = true }; } }

        public static BKICommand<T> SetVoltage(bool is100V = false, bool isMOm = false) => new BKICommand<T>(new byte[] { (byte)Command.Voltage, 0, (byte)((is100V ? 0x10 : 0) | (isMOm ? 0x01 : 0)) }) { MustBeEqualToRequest = true };
        public static BKICommand<T> CommutationOnTerminal { get { return new BKICommand<T>(new byte[] { (byte)Command.Commut, 0, 0 }) { MustBeEqualToRequest = true }; } }
        public static BKICommand<T> StartVoltageMeasure(float vmax) =>
            new BKICommand<T>(new byte[] {
                (byte)((byte)Command.MeasureV),
                0,
                (byte)(vmax * 10)
            })
            { MustBeEqualToRequest = true };
        public static BKICommand<T> StartResistMeasure(bool is100V = false, bool isMOm = false, bool withDisplay = false) =>
            new BKICommand<T>(new byte[] {
                (byte)((byte)Command.MeasureR | (withDisplay ? 0x01 : 0x00)),
                0,
                (byte)((is100V? 0x10 : 0) | (isMOm?0x01:0))
            })
            { MustBeEqualToRequest = true };

        public static BKICommand<T> Commutation(int board, int section, UInt16 mask) => new BKICommand<T>(new byte[] { (byte)((section << 3) | board), mask.Hi(), mask.Lo() }) { MustBeEqualToRequest = true };
        public static BKICommand<T> RegisterRead(byte reg) => new BKICommand<T>(new byte[] { (byte)Command.RegRead, reg, 0 }, answLength: 4);
        public static BKICommand<T> RegisterWrite(byte reg, byte value) => new BKICommand<T>(new byte[] { (byte)Command.RegWrite, reg, value }, answLength: 4) { MustBeEqualToRequest = false };



        public BKICommand(byte[] data, int answLength = 3) : base(data, answLength) { }


        public override byte[] ToArray() => data;

    }
}
