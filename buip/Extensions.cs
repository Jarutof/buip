using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace buip
{
    public static class Extensions
    {
        public static byte SetBit(this byte data, int n)
        {
            return data |= (byte)(1 << n);
        }
        public static UInt16 SetBit(this UInt16 data, int n)
        {
            return data |= (UInt16)(1 << n);
        }
        public static byte RemBit(this byte data, int n)
        {
            return data &= (byte)((1 << n) ^ 0xFF);
        }
        public static bool IsBit(this byte data, int n)
        {
            return (data & (1 << n)) != 0;
        }
        public static bool IsBit(this UInt16 data, int n)
        {
            return (data & (1 << n)) != 0;
        }
        public static byte[] ToArray(this UInt16 data)
        {
            return new byte[] { (byte)(data >> 8), (byte)data };
        }

        public static byte Hi(this UInt16 data)
        {
            return (byte)(data >> 8);
        }

        public static byte Lo(this UInt16 data)
        {
            return (byte)data;
        }
        public static float ToFloat(this UInt16 data)
        {
            int sign = data.IsBit(15) ? -1 : 1;
            return sign * (data & 0x7FFF) / 100f;
        }

        public static UInt16 ToUInt16(this float data, int accuracy = 2)
        {
            UInt16 res = ((UInt16)(Math.Abs(data) * Math.Pow(10, accuracy)));
            if (data < 0) res = res.SetBit(15);
            return res;
        }

        public static bool IsInArea(this float data, float center, float area)
        {
            return Math.Abs(data - center) < area;
        }
        public static bool IsInAreaPerCent(this float data, float center, float areaPC)
        {
            return Math.Abs(data - center) < (0.01f * areaPC * areaPC);
        }
        public static bool IsInRange(this float data, float min, float max)
        {
            return (data <= max) && (data >= min);
        }


        public static UInt16 GetUInt16(this byte[] data, int index)
        {
            return (UInt16)((data[index] << 8) | data[index + 1]);
        }
        public static UInt16 GetUInt16Invert(this List<byte> data, int index)
        {
            return (UInt16)((data[index + 1] << 8) | data[index]);
        }
        public static UInt16 GetUInt16(this List<byte> data, int index)
        {
            return (UInt16)((data[index] << 8) | data[index + 1]);
        }
        public static Int16 GetInt16(this List<byte> data, int index)
        {
            return (Int16)((data[index] << 8) | data[index + 1]);
        }
        public static float? GetSign(this float data)
        {
            if (data == 0) return null;
            return data / Math.Abs(data);
        }
        public static bool DeepEqual(this IEnumerable<float> data, IEnumerable<float> value)
        {
            List<float> dataList = new List<float>(data);
            List<float> valueList = new List<float>(value);
            if (dataList.Count != valueList.Count) return false;

            for (int i = 0; i < dataList.Count; i++)
            {
                if (dataList[i] != valueList[i]) return false;
            }
            return true;
        }

        public static bool DeepEqual(this IEnumerable<byte> data, IEnumerable<byte> value)
        {
            List<byte> dataList = new List<byte>(data);
            List<byte> valueList = new List<byte>(value);
            if (dataList.Count != valueList.Count) return false;

            for (int i = 0; i < dataList.Count; i++)
            {
                if (dataList[i] != valueList[i]) return false;
            }
            return true;
        }

        public static int[] GetBitNumbers(this UInt16 data)
        {
            List<int> numbers = new List<int>();
            for (int i = 0; i < 16; i++)
            {
                if (data.IsBit(i)) numbers.Add(i);
            }
            return numbers.ToArray();
        }

        public static byte[] ToArray(this float data)
        {
            return data.ToUInt16().ToArray();
        }

        public static IEnumerable<byte> AddCRC16(this IEnumerable<byte> src)
        {
            List<byte> srcList = new List<byte>(src);
            ushort CRCFull = 0xFFFF;
            char CRCLSB;

            for (int i = 0; i < srcList.Count; i++)
            {
                CRCFull = (ushort)(CRCFull ^ srcList[i]);

                for (int j = 0; j < 8; j++)
                {
                    CRCLSB = (char)(CRCFull & 0x0001);
                    CRCFull = (ushort)((CRCFull >> 1) & 0x7FFF);

                    if (CRCLSB == 1)
                        CRCFull = (ushort)(CRCFull ^ 0xA001);
                }
            }
            srcList.Add(CRCFull.Lo());
            srcList.Add(CRCFull.Hi());
            return srcList;
        }

        public static Task WaitForExitAsync(this Process process, CancellationToken cancellationToken = default(CancellationToken))
        {
            var tcs = new TaskCompletionSource<bool>();
            process.EnableRaisingEvents = true;
            process.Exited += (s, e) => tcs.TrySetResult(true);
            if (cancellationToken != default(CancellationToken))
                cancellationToken.Register(tcs.SetCanceled);

            return tcs.Task;
        }
    }
}
