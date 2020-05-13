using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace buip.Model
{
    internal class CableInfo
    {
        private readonly Dictionary<int, ushort> values = new Dictionary<int, ushort>();

        private string path;
        public CableInfo(int batt, int circ)
        {
            path = $"cable_B{batt}_C{circ}.cal";
            if (File.Exists(path))
            {
                var lines = File.ReadAllLines(path);
                foreach (string line in lines)
                {
                    var parts = line.Split(';');
                    if (parts.Length == 2)
                    {
                        if (int.TryParse(parts[0], out int number) && UInt16.TryParse(parts[1], out UInt16 value))
                        {
                            if (!values.ContainsKey(number))
                            {
                                values.Add(number, value);
                            }
                        }
                        else
                        {
                            Console.WriteLine($"CableInfo file {path}  parse failed");
                            File.Delete(path);
                            break;
                        }
                    }
                    else
                    {
                        Console.WriteLine($"CableInfo file {path}  parse failed");
                        File.Delete(path);
                        break;
                    }
                }
            }
            else
            {
                Console.WriteLine($"CableInfo file {path} not exists");
            }
        }

        public void CalibrateMeasure(byte number, ushort measure)
        {
            values[number] = measure;

            File.WriteAllLines(path, values.Select(item => $"{item.Key};{item.Value}"));
        }

        internal UInt16 GetCalibratedMeasure(byte number, ushort measure)
        {
            if (values.ContainsKey(number)) return (UInt16)Math.Max(measure - values[number], 0);
            else return measure;
        }
    }
}