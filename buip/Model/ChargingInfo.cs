using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;

namespace buip.Model
{
    public class ChargingInfo
    {
        private const string filename = "charging.cfg";

        // private const string keyVoltageSetting = "battery_voltage";
        private const string keyOvervoltage = "battery_overvoltage";
        private const string keyUndervoltage = "battery_undervoltage";
        private const string keyVoltageToCurrentUp = "voltage_to_current_up";
        private const string keyVoltageToCurrentDown = "voltage_to_current_down";
        private const string keyCurrentSetting = "battery_current";
        private const string keyOvercurrent = "battery_overcurrent";
        private const string keyMaxAkkVoltage = "akkumulator_overvoltage";

        private const string keyCurrentRangePerCent = "battery_current_range_percent";
        private const string keyCodeMax = "stabilizer_board_max_code";
        private const string keyCodeMin = "stabilizer_board_min_code";
        private const string keyCurrentToEnd = "current_when_mode_finished";
        private const string keyDuration = "duration_sec";
        private const string keyRecordsPeriod = "records_period_msec";



        private const string keySettingC = "current_and_code";
        private const string keySettingV = "voltage_and_code";

        private Dictionary<string, float> dictionaryInfo = new Dictionary<string, float>()
        {
            //[keyVoltageSetting] = 36.5f,
            [keyOvervoltage] = 36.865f,
            [keyUndervoltage] = 36.5f,
            [keyVoltageToCurrentUp] = 36.55f,
            [keyVoltageToCurrentDown] = 36.6f,
            [keyCurrentSetting] = 1f,
            [keyOvercurrent] = 1.01f,
            [keyMaxAkkVoltage] = 1.37f,

            [keyCurrentRangePerCent] = 0.5f,
            [keyCodeMax] = 1023f,
            [keyCodeMin] = 240f,
            [keyCurrentToEnd] = 0.05f,
            [keyDuration] = 10800f,
            [keyRecordsPeriod] = 300000f
        };

        public List<CodeSetting> CodeSettingsC { get; private set; } = new List<CodeSetting>();
        public List<CodeSetting> CodeSettingsV { get; private set; } = new List<CodeSetting>();

        public struct CodeSetting
        {
            public float Setting { get; set; }
            public UInt16 Code { get; set; }
        }

        //public float VoltageSetting { get { return dictionaryInfo[keyVoltageSetting]; } }
        public float Overvoltage { get { return dictionaryInfo[keyOvervoltage]; } }
        public float Undervoltage { get { return dictionaryInfo[keyUndervoltage]; } }
        public float VoltageToCurrentUp { get { return dictionaryInfo[keyVoltageToCurrentUp]; } }
        public float VoltageToCurrentDown { get { return dictionaryInfo[keyVoltageToCurrentDown]; } }

        public float CurrentSetting { get { return dictionaryInfo[keyCurrentSetting]; } }
        public float Overcurrent { get { return dictionaryInfo[keyOvercurrent]; } }
        public float MaxAkkVoltage { get { return dictionaryInfo[keyMaxAkkVoltage]; } }

        public float CurrentRangePerCent { get { return dictionaryInfo[keyCurrentRangePerCent]; } }
        public UInt16 CodeMax { get { return (UInt16)dictionaryInfo[keyCodeMax]; } }
        public UInt16 CodeMin { get { return (UInt16)dictionaryInfo[keyCodeMin]; } }
        public float CurrentToEnd { get { return dictionaryInfo[keyCurrentToEnd]; } }
        public UInt16 Duration { get { return (UInt16)dictionaryInfo[keyDuration]; } }
        public double RecordsPeriod { get { return dictionaryInfo[keyRecordsPeriod]; } }

        public ChargingInfo()
        {
            Load();
        }

        public void Save()
        {
            List<string> list = new List<string>();
            foreach (var kvp in dictionaryInfo)
            {
                list.Add(string.Format("{0}: {1}", kvp.Key, kvp.Value));
            }

            foreach (var cs in CodeSettingsC)
            {
                list.Add(string.Format("{0}: {1};{2};", keySettingC, cs.Setting, cs.Code));
            }
            foreach (var cs in CodeSettingsV)
            {
                list.Add(string.Format("{0}: {1};{2};", keySettingV, cs.Setting, cs.Code));
            }

            File.WriteAllLines(filename, list.ToArray());
        }

        public void Load()
        {

            if (!File.Exists(filename))
            {
                CodeSettingsC.Add(new CodeSetting() { Setting = 2, Code = 1 });
                CodeSettingsC.Add(new CodeSetting() { Setting = 0.9f, Code = 10 });
                CodeSettingsV.Add(new CodeSetting() { Setting = 40, Code = 1 });
                CodeSettingsV.Add(new CodeSetting() { Setting = 36f, Code = 5 });
                Save();
                return;
            }
            string[] lines = File.ReadAllLines(filename);
            foreach (var line in lines)
            {
                string[] parts = line.Replace(" ", "").Replace(",", ".").Split(':');
                if (dictionaryInfo.ContainsKey(parts[0]))
                {
                    if (parts.Length > 1 && float.TryParse(parts[1].Split(';')[0], NumberStyles.Float, CultureInfo.InvariantCulture, out float val))
                    {
                        dictionaryInfo[parts[0]] = val;
                    }
                }
                if (parts[0] == keySettingC)
                {
                    string[] cc = parts[1].Split(';');
                    if (cc.Length > 1)
                    {
                        if (float.TryParse(cc[0], NumberStyles.Float, CultureInfo.InvariantCulture, out float current) && UInt16.TryParse(cc[1], out UInt16 code))
                        {
                            CodeSettingsC.Add(new CodeSetting() { Setting = current, Code = code });
                        }

                    }
                }
                if (parts[0] == keySettingV)
                {
                    string[] cc = parts[1].Split(';');
                    if (cc.Length > 1)
                    {
                        if (float.TryParse(cc[0], NumberStyles.Float, CultureInfo.InvariantCulture, out float current) && UInt16.TryParse(cc[1], out UInt16 code))
                        {
                            CodeSettingsV.Add(new CodeSetting() { Setting = current, Code = code });
                        }

                    }
                }
            }

            if (CodeSettingsC.Count == 0)
            {
                CodeSettingsC.Add(new CodeSetting() { Setting = 1, Code = 1 });
                CodeSettingsC.Add(new CodeSetting() { Setting = 0.9f, Code = 10 });
            }
            if (CodeSettingsC.Count == 0)
            {
                CodeSettingsV.Add(new CodeSetting() { Setting = 40, Code = 1 });
                CodeSettingsV.Add(new CodeSetting() { Setting = 36f, Code = 5 });
            }
            CodeSettingsC.Sort((p, n) => (int)(n.Setting - p.Setting));
            CodeSettingsV.Sort((p, n) => (int)(n.Setting - p.Setting));
        }
    }
}
