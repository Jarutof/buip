using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace buip.Model.Operations
{
    public class OperationDiagnostic : Operation
    {
        public OperationDiagnostic(OperationInfo info) : base(info) { }
        public override async Task ProcessAsync(CancellationToken token)
        {
            Step.MaxValue = 20;
            Step.Value = 5;
            DataManager.Singleton.AddRecord(DataManager.RecordTypeEnum.RT_MODE, (byte)DataManager.EventTypeEnum.ET_PROCESS, 0);

            var result = await Task.WhenAll(DataManager.Singleton.Devices.OrderBy(k => k.Key).Select(d => d.Value.CheckAsync()));
            for (int i = 0; i < result.Length; i++)
            {
                Console.WriteLine($"{(DataManager.DevicesEnum)i} : {result[i]}");
            }

            if (result[(byte)DataManager.DevicesEnum.bki])
                DataManager.Singleton.AddRecord(DataManager.RecordTypeEnum.RT_TEST, (byte)DataManager.TestEventTypeEnum.ET_T_BV_BKI, 0);
            else
                DataManager.Singleton.AddRecord(DataManager.RecordTypeEnum.RT_ERROR, (byte)DataManager.EventTypeEnum.ET_BV_BKI_ERR, 0);

            Step.Value = Step.MaxValue;

            if (result.Where((r, i) => i != (int)DataManager.DevicesEnum.bki).All(r => r))
            {
                DataManager.Singleton.AddRecord(DataManager.RecordTypeEnum.RT_TEST, (byte)DataManager.TestEventTypeEnum.ET_T_BV_BUIP, 0);
            }
            else
            {
                DataManager.Singleton.AddRecord(DataManager.RecordTypeEnum.RT_ERROR, (byte)DataManager.EventTypeEnum.ET_BV_BUIP_ERR, 0);
            }

            if (result.All(r => r))
            {
                DataManager.Singleton.PlayComplete();

                DataManager.Singleton.AddRecord(DataManager.RecordTypeEnum.RT_TEST, (byte)DataManager.TestEventTypeEnum.ET_T_BV, 0);
            }
            else
            {
                DataManager.Singleton.PlayError();

                DataManager.Singleton.AddRecord(DataManager.RecordTypeEnum.RT_ERROR, (byte)DataManager.EventTypeEnum.ET_BV_ERR, 0);
            }

            DataManager.Singleton.AddRecord(DataManager.RecordTypeEnum.RT_MODE, (byte)DataManager.EventTypeEnum.ET_CONFIRMED, (byte)DataManager.EventTypeEnum.ET_CONFIRMED);

            DataManager.Singleton.EndOperation();
            Console.WriteLine("End of OperationDiagnostic");
        }



    }
}
