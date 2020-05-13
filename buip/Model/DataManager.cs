using buip.Model.Devices;
using buip.Model.Operations;
using buip.Shared;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace buip.Model
{


    public class DataManager
    {
        public enum DevicesEnum { bki, rele, stabilizer, adc0, adc1, adc2 }

        public enum CommandsPDUEnum
        {
            CMDFileHead = 0xFA,
            CMDFile = 0xFB,
            CMDFileEnd = 0xFC,
            CMDStatus = 0xD1,
            CMDSetMode = 0xD2,
            CMDRecord = 0xD3,
            CMDDateTime = 0xDD,
            CMDNRCCAB = 0x23,
            CMDNRCON = 0xE0,
            CMDNRCOFF = 0xEF,
            CMDGetMeas = 0xCA,
            CMDCalibr = 0xC0,
            CMDGetMeasures = 0xCB,
            CMDCalibrClear = 0xCC,
        }
        /* { Типы записей }*/
        public enum RecordTypeEnum
        {
            RT_MODE = 0,    // Только в журнал по режиму
            RT_RESIST = 1, // Замер сопротивления
            RT_NRC = 2,     // Напряжения аккумуляторов
            RT_VOLTAGE = 3, // Напряжения измерительных каналов
            RT_CURRENT = 4, // Ток
            RT_VSUMM = 5,   // Суммарное напряжение
            RT_ERROR = 6,   // Только в журнал неисправностей
            RT_JOURNAl = 7, // Только в лог
            RT_TEST = 8,    // Записи о ходе теста
            RT_VOLTAGE_BV = 9,    // Записи о ходе теста
            RT_VOLTAGES = 10,
            RT_CAPACITY = 11,
            RT_TIME = 12,
            RT_CURVOLTCAP = 14,
        }

        public enum ModeEventTypeEnum
        {
            ET_M_START = 0,
            ET_M_STOP = 1,
        }


        public enum ResistEventTypeEnum
        {
            ET_RES_OM = 0,
            ET_RES_MOM = 1,
            ET_RES_VOLT = 2,
        }



        public enum TestEventTypeEnum
        {
            ET_T_CONTBIN = 0,
            ET_T_CONTIM = 1,
            ET_T_RESBIN = 2,
            ET_T_RESIM = 3,
            ET_T_OTHERIM = 4,
            ET_T_BIN = 5,
            ET_T_BU = 6,
            ET_T_BKI = 7,
            ET_T_STAB = 8,
            ET_T_BKU = 9,
            ET_T_IMIT = 10,
            ET_T_BV = 11,
            ET_T_BV_BUIP = 12,
            ET_T_BV_BKI = 13,
        }


        public enum EventTypeEnum
        {
            ET_PROCESS = 0,
            ET_CONFIRMED = 1,
            ET_ABORT = 2,
            ET_BKI_EX = 3,
            ET_STAB_EX = 4,
            ET_IMIT_EX = 5,
            ET_BKI_ERR = 6,
            ET_STABIL_ERR = 7,
            ET_IMIT_ERR = 8,
            ET_ADC_ERR = 9,
            ET_BU_RELE_ERR = 10,
            ET_BU_PKV_ERR = 11,
            ET_BIN_OC = 12,
            ET_IMIT_OC = 13,
            ET_BKI_RELE_ERR = 14,
            ET_VMAX = 15,
            ET_VMIN = 16,
            ET_CURRENT_ERR = 17,
            ET_BU_RESTART = 18,
            ET_BIN_ERR = 19,
            ET_BIN_CONT_ERR = 20,
            ET_BIN_RESIST_ERR = 21,
            ET_STABIL_CONT_ERR = 22,
            ET_IMIT_CONT_ERR = 23,
            ET_BU_ERR = 24,
            ET_VOLTAGE = 25,
            ET_BKU_ERR = 26,
            ET_IMIT_RESIST_ERR = 27,
            ET_NOIZE = 28,
            ET_BU_ZALIPON = 29,
            ET_BU_NOTCOM = 30,
            ET_BU_BADVAL = 31,
            ET_BU_RELVAL = 32,
            ET_IMIT_TEST = 33,

            ET_BV_ERR = 34,
            ET_BV_BUIP_ERR = 35,
            ET_BV_BKI_ERR = 36,

            ET_BV_BKI_EX = 37,
            ET_BV_ADC_EX = 38,
            ET_BV_RELE_EX = 39,
            ET_BV_STAB_EX = 40,
            ET_BV_RELE_ERR = 41,

            ET_CHARGE_50 = 42,
            ET_CHARGE_AKK = 43,
            ET_CHARGE_TIME = 44,

        }

        public event EventHandler<TEventArgs<IEnumerable<byte>>> OnReadyPDUAnswer;

        public Dictionary<CommandsPDUEnum, Action<IEnumerable<byte>>> handlersPDU = new Dictionary<CommandsPDUEnum, Action<IEnumerable<byte>>>();

        private List<Record> Records { get; } = new List<Record>();
        public StatusInfo Status { get; } = new StatusInfo();
        public StabilizerDevice StabilizerDevice => Devices[DevicesEnum.stabilizer] as StabilizerDevice;
        public ReleDevice ReleDevice => Devices[DevicesEnum.rele] as ReleDevice;
        public BKIDevice BKIDevice => Devices[DevicesEnum.bki] as BKIDevice;
        public ADCDevice ADC_0_Device => Devices[DevicesEnum.adc0] as ADCDevice;
        public ADCDevice ADC_1_Device => Devices[DevicesEnum.adc1] as ADCDevice;
        public ADCDevice ADC_2_Device => Devices[DevicesEnum.adc2] as ADCDevice;

        private ADCDevice[] adcDevices;
        public ADCDevice[] ADCDevices
        {
            get
            {
                if (adcDevices == null)
                    adcDevices = new ADCDevice[] { ADC_0_Device, ADC_1_Device, ADC_2_Device };
                return adcDevices;
            }
        }
        public bool IsOperationInProcess { get => Status.EventType == EventTypeEnum.ET_PROCESS; }

        public List<CircuitTypeEnum> DiagnosticOperations { get; } = new List<CircuitTypeEnum>() { CircuitTypeEnum.CT_DT_BKCB, CircuitTypeEnum.CT_DT_BV, CircuitTypeEnum.CT_DT_CALI, CircuitTypeEnum.CT_DT_IMMIT, CircuitTypeEnum.CT_DT_CCON };
        public bool CanStopOperationIfError { get => Status.EventType == EventTypeEnum.ET_PROCESS && (Status.Operation != null && !DiagnosticOperations.Contains(Status.Operation.Info.CircuitType)); }

        public Dictionary<DevicesEnum, Device> Devices { get; private set; } = new Dictionary<DevicesEnum, Device>();

        public const float Сoefficient_2_5V = 2.5f / 0x7FFF;
        public const float Сoefficient_100mv = 100f / 0x7FFF;
        public const float СoefficientCurrent = 0.067f;

        public static DataManager Singleton;
        public static void Init()
        {
            Singleton = new DataManager();
        }

        public void AddDevice(DevicesEnum t, Device dev)
        {
            Devices.Add(t, dev);
        }
        public void StartOperation()
        {
            foreach (var kvp in Devices)
                kvp.Value.SetInitState();
            Status.EventType = EventTypeEnum.ET_PROCESS;
            Status.Operation.Start();
        }

        public void EndOperation()
        {
            if (Status.Operation != null) Status.Operation.Step.Value = 0;
            Status.EventType = EventTypeEnum.ET_CONFIRMED;
        }

        public void StopOperation(EventTypeEnum et)
        {
            Status.EventType = et;
            if (Status.Operation != null) Status.Operation.Step.Value = 0;
            Singleton.AddRecord(RecordTypeEnum.RT_MODE, (byte)ModeEventTypeEnum.ET_M_STOP, (byte)et);

        }

        public DataManager()
        {

            string fileName = string.Empty;
            List<byte> body = new List<byte>();
            handlersPDU.Add(CommandsPDUEnum.CMDFileHead, data =>
            {
                OnReadyPDUAnswer?.Invoke(this, new TEventArgs<IEnumerable<byte>>(data));
                body.Clear();
                fileName = Encoding.ASCII.GetString(data.ToArray(), 1, data.Count() - 2).TrimStart('\\');
                Console.WriteLine(fileName);
            });
            handlersPDU.Add(CommandsPDUEnum.CMDFile, data =>
            {
                Console.WriteLine($"body {data.Count()} {data.ToArray()[13]}");

                OnReadyPDUAnswer?.Invoke(this, new TEventArgs<IEnumerable<byte>>(new byte[] { (byte)CommandsPDUEnum.CMDFile }));
                body.AddRange(data.Where((b, id) => id > 0 && (id < (data.Count() - 1))));
            });
            handlersPDU.Add(CommandsPDUEnum.CMDFileEnd, data =>
            {
                OnReadyPDUAnswer?.Invoke(this, new TEventArgs<IEnumerable<byte>>(data));
                Console.WriteLine($"write file {fileName} {body.Count}");
                Task.Run(async () =>
                {
                    await SetWritePermission();
                    File.WriteAllBytes(fileName, body.ToArray());
                });
            });

            handlersPDU.Add(CommandsPDUEnum.CMDSetMode, data =>
            {
                Console.WriteLine("CMDSetMode");
                OnReadyPDUAnswer?.Invoke(this, new TEventArgs<IEnumerable<byte>>(data));

                OperationInfo operationInfo = new OperationInfo(data.ToList());
                Console.WriteLine($"operationInfo.ModeId = {operationInfo.ModeId} {operationInfo.CircuitType}");
                if (operationInfo.ModeId == 0)
                {
                    Console.WriteLine("operationInfo.ModeId = 0");
                    Singleton.Records.Clear();
                    Status.Operation?.StopAsync();
                    Status.Operation = Operation.Empty;
                }

                if (Status.Operation != null)
                {
                    if (Status.Operation.Info.ModeId != operationInfo.ModeId)
                    {
                        Console.WriteLine("Records.Clear()");
                        Singleton.Records.Clear();
                    }
                }

                if (operationInfo.Type == OperationTypeEnum.OP_STOP)
                {
                    Console.WriteLine("Status.Operation?.StopAsync()");

                    Status.Operation?.StopAsync();
                }
                else
                {

                    Status.Operation = Operation.Create(operationInfo);
                    StartOperation();
                }
            });

            handlersPDU.Add(CommandsPDUEnum.CMDStatus, data =>
            {
                OnReadyPDUAnswer?.Invoke(this, new TEventArgs<IEnumerable<byte>>(Status.ToArray()));
            });

            handlersPDU.Add(CommandsPDUEnum.CMDRecord, data =>
            {
                UInt16 number = data.ToList().GetUInt16(1);
                if (Records.Count > number)
                {
                    OnReadyPDUAnswer?.Invoke(this, new TEventArgs<IEnumerable<byte>>(Records.Find(r => r.Id == number).ToArray()));
                }
                else Console.WriteLine($"number > Records.Count: {Records.Count} > {number}");
            });

            handlersPDU.Add(CommandsPDUEnum.CMDDateTime, data =>
            {
                List<byte> list = data.ToList();
                DateTime dateTime = new DateTime(list[1] + 2000, list[2], list[3], list[4], list[5], list[6]);
                Console.WriteLine($"DateTime {dateTime}");
                DeltaTime.Set(dateTime);
                OnReadyPDUAnswer?.Invoke(this, new TEventArgs<IEnumerable<byte>>(data));
            });

        }

        private static bool IsWritePermission = false;
        private static DateTime writePermissionTimer;

        public static async Task SetWritePermission()
        {
            writePermissionTimer = DateTime.Now;
            if (!IsWritePermission)
            {
                IsWritePermission = true;
                await SetRemountRW(true);

                _ = Task.Run(async () =>
                {
                    while ((DateTime.Now - writePermissionTimer).TotalSeconds < 5)
                    {
                        await Task.Delay(100);
                    }
                    await SetRemountRW(false);
                    IsWritePermission = false;

                });
            }
        }

        private static async Task SetRemountRW(bool state)
        {
            try
            {
                using (Process myProcess = new Process())
                {
                    myProcess.StartInfo.FileName = state ? "remountrw" : "remountro";
                    if (myProcess.Start())
                    {
                        Console.WriteLine($"myProcess.WaitForExitAsync {state}");
                        await myProcess.WaitForExitAsync();
                        Console.WriteLine("WaitForExitAsync");

                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
        }

        public void SetPDUData(ICommand<ComPort.StatusEnum> cmd)
        {
            if (handlersPDU.ContainsKey((CommandsPDUEnum)cmd.Data.Buffer[0])) handlersPDU[(CommandsPDUEnum)cmd.Data.Buffer[0]](cmd.Data.Buffer);
        }
        public class StatusInfo
        {
            private TaskCompletionSource<bool> currentCompletionSource;
            public EventTypeEnum EventType { get; set; }
            public Operation Operation { get; set; }
            public float Capacity { get; set; }
            public IEnumerable<float> Voltages
            {
                get => Singleton.ADCDevices.SelectMany((dev, devId) => dev.Voltages.Where((f, vId) => (devId * 10 + vId) < 27));
            }

            public float VoltageSumm
            {
                get => Voltages.Sum();
            }
            public float Current
            {
                get => Math.Abs(Singleton.ADC_2_Device.Voltages[9] * СoefficientCurrent);
            }

            public byte[] ToArray()
            {
                var bki = Singleton.BKIDevice as BKIDevice;
                List<byte> list = new List<byte>();
                list.Add((byte)CommandsPDUEnum.CMDStatus);
                list.Add((byte)EventType);

                list.AddRange(Operation?.ToArray() ?? Operation.Empty.ToArray());
                if (Singleton.Records.Count == 0)
                    list.AddRange(((UInt16)0).ToArray());
                else
                    list.AddRange(((UInt16)(Singleton.Records.Last().Id + 1)).ToArray());
                list.AddRange(bki.Codes);

                list.AddRange(Singleton.ADCDevices.SelectMany(dev => dev.Voltages).SelectMany(v => BitConverter.GetBytes(v)).ToArray());
                list.AddRange(BitConverter.GetBytes(VoltageSumm));
                list.AddRange(BitConverter.GetBytes(Current));
                list.AddRange(BitConverter.GetBytes(Capacity));

                return list.ToArray();
            }


            public async Task<bool> NewVoltagesAsync()
            {
                var savedVoltages0 = new List<float>(Singleton.ADC_0_Device.Voltages);
                var savedVoltages1 = new List<float>(Singleton.ADC_1_Device.Voltages);
                var savedVoltages2 = new List<float>(Singleton.ADC_2_Device.Voltages.Where((f, id) => ((id != 7) && (id != 8))));
                bool b0 = false;
                bool b1 = false;
                bool b2 = false;
                DateTime dt = DateTime.Now;
                Stopwatch sw = new Stopwatch();
                sw.Start();
                while (!(b0 && b1 && b2) && ((DateTime.Now - dt).TotalMilliseconds < 1000))
                {
                    var adcStatusResults = await Task.WhenAll(Singleton.ADCDevices.Select(adc => adc.NewStatusAsync()));
                    if (!adcStatusResults.All(r => r)) return await Task.FromResult(false);

                    b0 = savedVoltages0.DeepEqual(Singleton.ADC_0_Device.Voltages);
                    b1 = savedVoltages1.DeepEqual(Singleton.ADC_1_Device.Voltages);
                    b2 = savedVoltages2.DeepEqual(Singleton.ADC_2_Device.Voltages.Where((f, id) => ((id != 7) && (id != 8))));
                }
                Console.WriteLine($"NewVoltagesAsync {sw.Elapsed.TotalMilliseconds}ms");
                return await Task.FromResult(true);
            }
            public async Task<bool> NewCurrentAsync()
            {
                float savedCurrent = Current;
                DateTime dt = DateTime.Now;
                while ((savedCurrent == Current) && ((DateTime.Now - dt).TotalSeconds < 5))
                {
                    if (!await Singleton.ADC_2_Device.NewStatusAsync()) return await Task.FromResult(false);
                }
                return await Task.FromResult(true);
            }
        }

        public void AddVoltagesRecord()
        {
            Records.Add(new Record
            {
                Type = RecordTypeEnum.RT_VOLTAGES,
                EventType = 0,
                BaterytKind = Status.Operation?.Info.BatteryKind ?? 0,
                BatteryId = Status.Operation?.Info.BatteryId ?? 0,
                CircuitType = Status.Operation?.Info.CircuitType ?? CircuitTypeEnum.CT_NONE,
                CircuitNumber = Status.Operation?.Info.CircuitNumber ?? 0,
                ModeId = Status.Operation?.Info.ModeId ?? 0,
                Time = DeltaTime.Get(),
                Id = (UInt16)Records.Count(),
                Value = Status.Voltages.SelectMany(v => BitConverter.GetBytes(v)).ToArray(),

            });
        }
        public void AddRecord(RecordTypeEnum t, byte e, params UInt16[] values)
        {
            Records.Add(new Record
            {
                Type = t,
                EventType = e,
                Value = values.SelectMany(v => v.ToArray()).ToArray(),
                BaterytKind = Status.Operation?.Info.BatteryKind ?? 0,
                BatteryId = Status.Operation?.Info.BatteryId ?? 0,
                CircuitType = Status.Operation?.Info.CircuitType ?? CircuitTypeEnum.CT_NONE,
                CircuitNumber = Status.Operation?.Info.CircuitNumber ?? 0,
                ModeId = Status.Operation?.Info.ModeId ?? 0,
                Time = DeltaTime.Get(),
                Id = (UInt16)Records.Count()
            });
        }
       
        public void AddRecord(RecordTypeEnum t, byte e, byte[] value)
        {
            Records.Add(new Record
            {
                Type = t,
                EventType = e,
                Value = value,
                BaterytKind = Status.Operation?.Info.BatteryKind ?? 0,
                BatteryId = Status.Operation?.Info.BatteryId ?? 0,
                CircuitType = Status.Operation?.Info.CircuitType ?? CircuitTypeEnum.CT_NONE,
                CircuitNumber = Status.Operation?.Info.CircuitNumber ?? 0,
                ModeId = Status.Operation?.Info.ModeId ?? 0,
                Time = DeltaTime.Get(),
                Id = (UInt16)Records.Count()
            });
        }
       
        public class Record
        {
            public UInt16 Id { get; set; }
            public RecordTypeEnum Type { get; set; }
            public byte EventType { get; set; }
            public byte[] Value { get; set; }
            public byte BaterytKind { get; set; }
            public UInt16 BatteryId { get; set; }
            public UInt16 ModeId { get; set; }
            public CircuitTypeEnum CircuitType { get; set; }
            public byte CircuitNumber { get; set; }
            public DateTime Time { get; set; }

            public byte[] ToArray()
            {
                List<byte> res = new List<byte>();
                res.Add((byte)CommandsPDUEnum.CMDRecord);
                res.AddRange(BatteryId.ToArray());
                res.AddRange(ModeId.ToArray());
                res.AddRange(Id.ToArray());

                res.Add(BaterytKind);
                res.Add((byte)Type);
                res.Add(EventType);
                res.Add((byte)CircuitType);
                res.Add(CircuitNumber);

                res.Add((byte)(Time.Year - 2000));
                res.Add((byte)(Time.Month));
                res.Add((byte)(Time.Day));
                res.Add((byte)(Time.Hour));
                res.Add((byte)(Time.Minute));
                res.Add((byte)(Time.Second));
                res.AddRange(((UInt16)(Time.Millisecond)).ToArray());

                res.AddRange(Value);

                return res.ToArray();
            }
        }
        private static class DeltaTime
        {
            static double deltaTime = 0;
            public static void Set(DateTime dateTime)
            {
                deltaTime = (dateTime - DateTime.Now).TotalMilliseconds;
            }
            public static DateTime Get()
            {
                return DateTime.Now.AddMilliseconds(deltaTime);
            }
        }
        public void PlayComplete()
        {
            Play(" -f 400 -l 60 -n -f 100 -l 60 -n -f 400 -l 60 -n -f 100 -l 60 -d 200 -n -f 600 -l 60 -n -f 800 -n -l 60 -f 600 -l 60 -n -f 800 -l 60");
        }
        public void PlayError()
        {
            Play(" -f 120 -l 80 -D 20 -n -f 120 -l 80 -D 20 -n -f 120 -l 80 -D 20 -n -f 100 -l 500");
        }
        internal void Play(string v)
        {
            try
            {
                using (Process myProcess = new Process())
                {
                    myProcess.StartInfo.FileName = "beep";
                    myProcess.StartInfo.Arguments = v;
                    myProcess.Start();
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
        }
        public void Play(int f, int l, int d)
        {
            try
            {
                using (Process myProcess = new Process())
                {
                    myProcess.StartInfo.FileName = "beep";
                    myProcess.StartInfo.Arguments = $" -l {l} -f {f} -D {d} -n";
                    myProcess.Start();
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }

        }
    }

}
