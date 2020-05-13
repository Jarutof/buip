using buip.Model.Devices;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace buip.Model.Operations
{
    internal class OperationCharging : Operation
    {
        public OperationCharging(OperationInfo info) : base(info)
        {

        }
        bool IsSomeAkkCharged(float vAkk) => DataManager.Singleton.Status.Voltages.Any(v => v > vAkk);

        DateTime capacityDateTime;
        private void CalculateCapacity(float cur)
        {
            DataManager.Singleton.Status.Capacity += (float)(DateTime.Now - capacityDateTime).TotalMilliseconds * cur / 3600000f;
            capacityDateTime = DateTime.Now;
        }

        private void AddRecordsPack(float vsum, float curr)
        {
            DataManager.Singleton.AddRecord(DataManager.RecordTypeEnum.RT_CURVOLTCAP, 0,
                vsum.ToUInt16(),
                curr.ToUInt16(),
                DataManager.Singleton.Status.Capacity.ToUInt16());

            DataManager.Singleton.AddVoltagesRecord();
        }

        public override async Task ProcessAsync(CancellationToken token)
        {

            Console.WriteLine("OperationCharging");
            DataManager.Singleton.AddRecord(DataManager.RecordTypeEnum.RT_MODE, (byte)DataManager.ModeEventTypeEnum.ET_M_START, 0);

            var stab = DataManager.Singleton.StabilizerDevice;
            var rele = DataManager.Singleton.ReleDevice;
            var adcs = DataManager.Singleton.ADCDevices;
            List<Device> devices = new List<Device>(adcs)
            {
                stab,
                rele
            };
            await DataManager.SetWritePermission();
            ChargingInfo chargingInfo = new ChargingInfo();


            UInt16 stabcode = chargingInfo.CodeMin; 

            for (int i = 0; i < 10; i++)
            {
                if (!await rele.OnAsync(i)) return;
            }
            capacityDateTime = DateTime.Now;
            DataManager.Singleton.Status.Capacity = 0;
            DateTime dt = DateTime.Now;
            Step.MaxValue = chargingInfo.Duration;
            Step.Value = 0;
            System.Timers.Timer timer = new System.Timers.Timer(chargingInfo.RecordsPeriod);

            timer.Elapsed += (s, e) =>
            {
                if (!isStarted)
                {
                    timer.Stop();
                    Console.WriteLine("Records timer stoped");
                    return;
                }
                Console.WriteLine("AddRecords");
                AddRecordsPack(DataManager.Singleton.Status.VoltageSumm, DataManager.Singleton.Status.Current);
            };
            timer.Start();
            var status = DataManager.Singleton.Status;

            if (!await status.NewVoltagesAsync())
            {
                Console.WriteLine("NewVoltagesAsync fail");
                await Task.WhenAll(devices.Select(d => d.InitAsync()));
                return;
            }

            DataManager.Singleton.AddRecord(DataManager.RecordTypeEnum.RT_VSUMM, 0, DataManager.Singleton.Status.VoltageSumm.ToUInt16());
            DataManager.Singleton.AddVoltagesRecord();

            float vsumm;
            float curr;
            bool isCurrentReached = false;
            bool isVoltageReached = false;
            while (Step.Value < Step.MaxValue) //3 hours
            {
                Step.Value = (UInt16)(DateTime.Now - dt).TotalSeconds;

                if (!await status.NewVoltagesAsync())
                {
                    Console.WriteLine("NewVoltagesAsync fail");
                    await Task.WhenAll(devices.Select(d => d.InitAsync()));
                    return;
                }
                vsumm = DataManager.Singleton.Status.VoltageSumm;
                curr = DataManager.Singleton.Status.Current;
                CalculateCapacity(curr);
                Console.WriteLine($"VoltageSumm: { vsumm.ToString("0.000")} Current: { curr.ToString("0.000")} ");
                for (int i = 0; i < DataManager.Singleton.ADCDevices.Length; i++)
                {
                    Console.Write($"ADC {i + 1}: ");
                    for (int j = 0; j < DataManager.Singleton.ADCDevices[i].Voltages.Length; j++)
                    {
                        Console.Write($" {DataManager.Singleton.ADCDevices[i].Voltages[j].ToString("0.000")}");
                    }
                    Console.WriteLine();
                }
                #region check on cancel
                if (vsumm >= (chargingInfo.Overvoltage))
                {
                    DataManager.Singleton.AddRecord(DataManager.RecordTypeEnum.RT_TIME, 0, Step.Value);
                    AddRecordsPack(vsumm, curr);

                    DataManager.Singleton.StopOperation(DataManager.EventTypeEnum.ET_VMAX);
                    await Task.WhenAll(devices.Select(d => d.InitAsync()));
                    DataManager.Singleton.PlayError();
                    return;
                }
                if (isVoltageReached && vsumm <= (chargingInfo.Undervoltage))
                {
                    DataManager.Singleton.AddRecord(DataManager.RecordTypeEnum.RT_TIME, 0, Step.Value);
                    AddRecordsPack(vsumm, curr);
                    DataManager.Singleton.StopOperation(DataManager.EventTypeEnum.ET_VMIN);
                    await Task.WhenAll(devices.Select(d => d.InitAsync()));
                    DataManager.Singleton.PlayError();
                    return;
                }

                if (curr >= chargingInfo.Overcurrent)
                {
                    AddRecordsPack(vsumm, curr);
                    DataManager.Singleton.AddRecord(DataManager.RecordTypeEnum.RT_TIME, 0, Step.Value);
                    DataManager.Singleton.StopOperation(DataManager.EventTypeEnum.ET_CURRENT_ERR);
                    await Task.WhenAll(devices.Select(d => d.InitAsync()));
                    DataManager.Singleton.PlayError();
                    return;
                }

                if (token.IsCancellationRequested)
                {
                    AddRecordsPack(vsumm, curr);
                    DataManager.Singleton.AddRecord(DataManager.RecordTypeEnum.RT_TIME, 0, Step.Value);
                    DataManager.Singleton.StopOperation(DataManager.EventTypeEnum.ET_ABORT);
                    await Task.WhenAll(devices.Select(d => d.InitAsync()));
                    DataManager.Singleton.PlayComplete();
                    return;
                }

                if (IsSomeAkkCharged(chargingInfo.MaxAkkVoltage))
                {
                    AddRecordsPack(vsumm, curr);
                    DataManager.Singleton.AddRecord(DataManager.RecordTypeEnum.RT_TIME, 0, Step.Value);
                    DataManager.Singleton.StopOperation(DataManager.EventTypeEnum.ET_CHARGE_AKK);
                    await Task.WhenAll(devices.Select(d => d.InitAsync()));
                    Console.WriteLine("End OperationCharging");
                    DataManager.Singleton.PlayComplete();
                    return;
                }
                if (isCurrentReached && curr < chargingInfo.CurrentToEnd)
                {
                    Console.WriteLine("Current < 0.05");
                    AddRecordsPack(vsumm, curr);
                    DataManager.Singleton.AddRecord(DataManager.RecordTypeEnum.RT_TIME, 0, Step.Value);
                    DataManager.Singleton.StopOperation(DataManager.EventTypeEnum.ET_CHARGE_50);
                    await Task.WhenAll(devices.Select(d => d.InitAsync()));
                    DataManager.Singleton.PlayComplete();
                    return;
                }
                #endregion

                if (vsumm < chargingInfo.VoltageToCurrentUp)
                {
                    if (!curr.IsInAreaPerCent(chargingInfo.CurrentSetting, chargingInfo.CurrentRangePerCent))
                    {
                        var kC = chargingInfo.CodeSettingsC.Where(cs => cs.Setting > curr).Last().Code;
                        var kV = chargingInfo.CodeSettingsV.Where(cs => cs.Setting > vsumm).Last().Code;
                        UInt16 k = Math.Min(kC, kV);
                        stabcode += (UInt16)(Math.Sign(chargingInfo.CurrentSetting - curr) * k);
                        stabcode = Math.Min(stabcode, chargingInfo.CodeMax);
                        Console.WriteLine($"stabcode {stabcode}");

                        if (!await stab.SetCode(stabcode))
                        {
                            Console.WriteLine("SetCode fail");
                            await Task.WhenAll(devices.Select(d => d.InitAsync()));
                            return;
                        }

                    }
                }
                else
                if (vsumm > chargingInfo.VoltageToCurrentDown)
                {
                    stabcode += (UInt16)(Math.Sign(chargingInfo.VoltageToCurrentDown - vsumm));
                    stabcode = Math.Min(stabcode, chargingInfo.CodeMax);
                    Console.WriteLine($"stabcode {stabcode}");

                    if (!await stab.SetCode(stabcode))
                    {
                        Console.WriteLine("SetCode fail");
                        await Task.WhenAll(devices.Select(d => d.InitAsync()));
                        return;
                    }
                }

                if (!isCurrentReached)
                {
                    isCurrentReached = curr.IsInAreaPerCent(chargingInfo.CurrentSetting, chargingInfo.CurrentRangePerCent);
                }
                if (!isVoltageReached)
                {
                    isVoltageReached = vsumm > chargingInfo.Undervoltage;
                    if (isVoltageReached)
                        isCurrentReached = true;
                }
            }
            AddRecordsPack(DataManager.Singleton.Status.VoltageSumm, DataManager.Singleton.Status.Current);
            DataManager.Singleton.AddRecord(DataManager.RecordTypeEnum.RT_TIME, 0, Step.Value);
            DataManager.Singleton.StopOperation(DataManager.EventTypeEnum.ET_CHARGE_TIME);
            await Task.WhenAll(devices.Select(d => d.InitAsync()));
            DataManager.Singleton.EndOperation();
            DataManager.Singleton.PlayComplete();
            Console.WriteLine("End OperationCharging");
        }
    }
}
