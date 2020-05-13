using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace buip.Model.Operations
{

    public abstract class Operation
    {
        public class Progress
        {
            public UInt16 Value;
            public UInt16 MaxValue;
        }
        protected CancellationTokenSource Source = new CancellationTokenSource();
        protected TaskCompletionSource<bool> tcs;

        public Progress Step { get; set; } = new Progress();

        public OperationInfo Info { get; private set; }
        protected bool isStarted;
        public abstract Task ProcessAsync(CancellationToken token);
        public Operation(OperationInfo info)
        {
            Info = info;
        }
        public static Operation CreateDiagnosticOperation()
        {
            return new OperationDiagnostic(new OperationInfo());
        }


        public static Operation CreateCircuitOperation()
        {
            return new OperationCircuit(new OperationInfo() { BatteryKind = 12, CircuitType = CircuitTypeEnum.CT_CON_F });
        }
        public static Operation Create(OperationInfo info)
        {
            switch (info.CircuitType)
            {
                case CircuitTypeEnum.CT_NONE: return new EmptyOperation(info);
                case CircuitTypeEnum.CT_DT_BV: return new OperationDiagnostic(info);
                case CircuitTypeEnum.CT_INS:
                case CircuitTypeEnum.CT_INS_F:
                case CircuitTypeEnum.CT_CON_F:
                case CircuitTypeEnum.CT_HEAT:
                    return new OperationCircuit(info);
                case CircuitTypeEnum.CT_CON:
                    return new OperationMeasureVoltages(info);
                case CircuitTypeEnum.CT_CHARGING:
                    return new OperationCharging(info);
                default: throw new Exception($"wrong operation {info}");
            }
        }

        public byte[] ToArray()
        {

            List<byte> array = new List<byte>(Info.ToArray());
            array.AddRange(Step.Value.ToArray());
            array.AddRange(Step.MaxValue.ToArray());
            return array.ToArray();
        }


        private static Operation emptyOperation;
        public static Operation Empty
        {
            get
            {
                if (emptyOperation == null)
                {

                    emptyOperation = Create(new OperationInfo());

                }
                return emptyOperation;
            }
        }
        static Task process;
        public void Start()
        {
            Console.WriteLine($"process {process?.Status}");
            process = Task.Run(async () =>
            {
                Console.WriteLine("Task.Run");

                isStarted = true;
                tcs = new TaskCompletionSource<bool>();
                try
                {
                    await ProcessAsync(Source.Token);
                    Console.WriteLine("Operation finisht");
                }
                catch (Exception e)
                {
                    Console.WriteLine("Operation fault");

                    Console.WriteLine(e);
                }
                isStarted = false;
                tcs.SetResult(true);
            }, Source.Token);
        }

        public async Task StopAsync()
        {
            Source.Cancel();
            await tcs.Task;
        }

    }
}
