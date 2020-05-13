using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace buip.Model.Operations
{
    public class OperationMeasureVoltages : Operation
    {
        public OperationMeasureVoltages(OperationInfo info) : base(info)
        {
        }

        public override async Task ProcessAsync(CancellationToken token)
        {
            Console.WriteLine("OperationMeasureVoltages");
            var adcs = DataManager.Singleton.ADCDevices;
            Step.MaxValue = 1;
            Step.Value = 1;

            var adcStatusResults = await Task.WhenAll(adcs.Select(adc => adc.NewStatusAsync()));
            if (!adcStatusResults.All(r => r)) return;

            var voltages = new List<float>(DataManager.Singleton.Status.Voltages);
            for (int i = 0; i < 27; i++)
            {
                Info.CircuitNumber = (byte)(i + 1);
                DataManager.Singleton.AddRecord(DataManager.RecordTypeEnum.RT_VOLTAGE_BV, (byte)(i + 1), BitConverter.GetBytes(voltages[i]));
            }

            DataManager.Singleton.EndOperation();
            Console.WriteLine("End OperationMeasureVoltages");
        }
    }
}
