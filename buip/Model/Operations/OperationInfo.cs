using System;
using System.Collections.Generic;

namespace buip.Model.Operations
{
    public enum ProcessEnum
    {
        Diagnostic
    }

    public enum CircuitTypeEnum
    {
        CT_NONE = 0,
        CT_INS_F = 1,
        CT_INS = 2,
        CT_CON_F = 3,
        CT_CON = 4,
        CT_HEAT = 5,
        CT_NRC1 = 6,
        CT_NRC2 = 7,
        CT_CCON1 = 8,
        CT_CCON2 = 9,
        CT_CCON3 = 10,
        CT_CCON4 = 11,
        CT_CCON5 = 12,
        CT_CCON6 = 13,
        CT_CCON7 = 14,
        CT_CCON8 = 15,
        CT_CCON9 = 16,
        CT_CCON10 = 17,
        CT_NRC3 = 18,
        CT_NRC4 = 19,
        CT_DT_BKCB = 20,
        CT_DT_CALI = 21,
        CT_DT_CCON = 22,
        CT_DT_IMMIT = 23,
        CT_DT_BV = 24,
        CT_CHARGING = 30

    }
    public enum OperationTypeEnum
    {
        OP_AUTO = 0,
        OP_MEASURE = 1,
        OP_COMMUTATION = 2,
        OP_RESET = 3,
        OP_STOP = 4,
        OP_DIAG = 5,
    }
    public class OperationInfo
    {
        public OperationTypeEnum Type { get; set; }

        public UInt16 BatteryId { get; set; }
        public byte BatteryKind { get; set; }
        public CircuitTypeEnum CircuitType { get; set; }
        public byte CircuitNumber { get; set; }
        public UInt16 ModeId { get; set; }
        public bool IsImmitator { get; set; }

        public OperationInfo(List<byte> data)
        {
            BatteryId = data.GetUInt16(1);
            ModeId = data.GetUInt16(3);
            IsImmitator = data[5] != 0;
            BatteryKind = data[6];
            CircuitType = (CircuitTypeEnum)data[7];
            CircuitNumber = data[8];
            Type = (OperationTypeEnum)data[9];
        }

        public byte[] ToArray()
        {
            List<byte> array = new List<byte>();
            array.AddRange(BatteryId.ToArray());
            array.AddRange(ModeId.ToArray());
            array.Add((byte)CircuitType);
            array.Add(CircuitNumber);
            array.Add((byte)Type);
            array.Add((byte)(IsImmitator ? 1 : 0));

            return array.ToArray();
        }

        public OperationInfo()
        {
        }

        public override string ToString()
        {
            return CircuitType.ToString();
        }
    }
}
