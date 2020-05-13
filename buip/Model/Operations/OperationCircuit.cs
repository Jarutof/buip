using buip.Model.Devices;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace buip.Model.Operations
{
    public class OperationCircuit : Operation
    {
        public OperationCircuit(OperationInfo info) : base(info)
        {
        }

        private async Task<bool> Measure(BatteryInfo.Circuit circuit, CableInfo ci)
        {
            var bki = DataManager.Singleton.BKIDevice;
            bool is100V = (Info.CircuitType == CircuitTypeEnum.CT_INS_F) || (Info.CircuitType == CircuitTypeEnum.CT_INS);
            bool isMOm = (Info.CircuitType == CircuitTypeEnum.CT_INS_F) || (Info.CircuitType == CircuitTypeEnum.CT_INS);

            if (!await bki.InitAsync()) return await Task.FromResult(false);

            if (!await CommutationCircuit(circuit))
            {
                await bki.InitAsync();
                return await Task.FromResult(false);
            }

            var measureResult = await MeasureCircuitAsync(is100V, isMOm);
            if (!measureResult.IsOk)
            {
                await bki.InitAsync();
                return await Task.FromResult(false);
            }


            Console.WriteLine($"result: {measureResult.Measure}");
            if (measureResult.Measure != 0xFFFF && measureResult.Measure != 0xFFFE)
                switch (Info.CircuitType)
                {
                    case CircuitTypeEnum.CT_CON:
                    case CircuitTypeEnum.CT_CON_F:
                    case CircuitTypeEnum.CT_HEAT:
                        if (Info.IsImmitator)
                        {
                            if (measureResult.Measure < 600) // < 6 Ом
                            {
                                await DataManager.SetWritePermission();
                                ci.CalibrateMeasure(Info.CircuitNumber, (UInt16)measureResult.Measure);
                            }
                        }
                        else
                            measureResult.Measure = ci.GetCalibratedMeasure(Info.CircuitNumber, measureResult.Measure);
                        break;
                }

            DataManager.Singleton.AddRecord(DataManager.RecordTypeEnum.RT_RESIST, (byte)(measureResult.IsOM ? DataManager.ResistEventTypeEnum.ET_RES_OM : DataManager.ResistEventTypeEnum.ET_RES_MOM), measureResult.Measure);
            return await Task.FromResult(true);
        }

        private async Task<bool> OperationMeasure(CancellationToken token)
        {
            BatteryInfo bi = new BatteryInfo(Info.BatteryKind);
            await DataManager.SetWritePermission();
            CableInfo ci = new CableInfo(Info.BatteryKind, (int)Info.CircuitType);
            var operation = bi.Operations.Where(op => op.Header.Number == (int)Info.CircuitType).First() as BatteryInfo.CircuitOperation;
            if (!await Measure(operation.Circuits[Info.CircuitNumber - 1], ci))
            {
                DataManager.Singleton.EndOperation();
                return await Task.FromResult(false);
            }
            DataManager.Singleton.EndOperation();
            return await Task.FromResult(true);
        }
        private async Task<bool> OperationAuto(CancellationToken token)
        {
            var bki = DataManager.Singleton.BKIDevice;
            BatteryInfo bi = new BatteryInfo(Info.BatteryKind);
            await DataManager.SetWritePermission();
            CableInfo ci = new CableInfo(Info.BatteryKind, (int)Info.CircuitType);
            DataManager.Singleton.AddRecord(DataManager.RecordTypeEnum.RT_MODE, (byte)DataManager.ModeEventTypeEnum.ET_M_START, 0);
            var operation = bi.Operations.Where(op => op.Header.Number == (int)Info.CircuitType).First() as BatteryInfo.CircuitOperation;
            Step.MaxValue = (UInt16)operation.Circuits.Count;
            for (int i = 0; i < operation.Circuits.Count; i++)
            {
                Step.Value = (UInt16)(i + 1);
                Info.CircuitNumber = (byte)Step.Value;
                Console.WriteLine($"Step = {Step.Value} / {Step.MaxValue}");

                if (!await Measure(operation.Circuits[i], ci))
                {
                    return await Task.FromResult(false);
                }

                if (token.IsCancellationRequested)
                {
                    if (!await bki.InitAsync()) return await Task.FromResult(false);
                    DataManager.Singleton.StopOperation(DataManager.EventTypeEnum.ET_ABORT);
                    return await Task.FromResult(false);
                }
            }
            DataManager.Singleton.StopOperation(DataManager.EventTypeEnum.ET_CONFIRMED);

            return await Task.FromResult(true);
        }

        private async Task<bool> OperationCommutation(CancellationToken token)
        {
            var bki = DataManager.Singleton.BKIDevice;
            BatteryInfo bi = new BatteryInfo(Info.BatteryKind);
            var operation = bi.Operations.Where(op => op.Header.Number == (int)Info.CircuitType).First() as BatteryInfo.CircuitOperation;
            bool is100V = (Info.CircuitType == CircuitTypeEnum.CT_INS_F) || (Info.CircuitType == CircuitTypeEnum.CT_INS);
            bool isMOm = (Info.CircuitType == CircuitTypeEnum.CT_INS_F) || (Info.CircuitType == CircuitTypeEnum.CT_INS);

            if (!await bki.InitAsync()) return await Task.FromResult(false);
            if (!await CommutationCircuit(operation.Circuits[Info.CircuitNumber - 1])) return await Task.FromResult(false);

            var resistResult = await bki.GetResistAsync(is100V, isMOm, withDisplay: true);
            if (!resistResult.IsOk)
            {
                return await Task.FromResult(false);
            }

            if (!await bki.CommutationOnTerminalAsync())
            {
                return await Task.FromResult(false);
            }
            return await Task.FromResult(true);
        }

        private async Task<bool> OperationDecommutation(CancellationToken token)
        {
            var bki = DataManager.Singleton.BKIDevice;
            var resistResult = await bki.GetResistAsync(false, false);
            DataManager.Singleton.EndOperation();
            return await DataManager.Singleton.BKIDevice.InitAsync();
        }

        private static Dictionary<OperationTypeEnum, Task<bool>> operations = new Dictionary<OperationTypeEnum, Task<bool>>();

        public override async Task ProcessAsync(CancellationToken token)
        {
            Console.WriteLine("OperationCircuit");

            switch (Info.Type)
            {
                case OperationTypeEnum.OP_AUTO: await OperationAuto(token); break;
                case OperationTypeEnum.OP_MEASURE: await OperationMeasure(token); break;
                case OperationTypeEnum.OP_COMMUTATION: await OperationCommutation(token); break;
                case OperationTypeEnum.OP_RESET: await OperationDecommutation(token); break;
            }

            Console.WriteLine("End OperationCircuit");
        }

        private async Task<BKIDevice.ResistMeasureResult> MeasureCircuitAsync(bool is100V, bool isMOm)
        {
            Console.WriteLine("MeasureCircuit");
            var bki = DataManager.Singleton.BKIDevice;


            if (!await bki.SetVoltageAsync(is100V, isMOm))
            {
                await bki.InitAsync();
                return await Task.FromResult(BKIDevice.ResistMeasureResult.Failed);
            }
            await Task.Delay(100);

            var resistResult = await bki.GetResistAsync(is100V, isMOm);

            if (!resistResult.IsOk) return await Task.FromResult(BKIDevice.ResistMeasureResult.Failed);
            await Task.Delay(50);
           
            if (!await bki.InitAsync()) return await Task.FromResult((BKIDevice.ResistMeasureResult.Failed));


            return await Task.FromResult(resistResult);
        }


        private async Task<bool> CommutationCircuit(BatteryInfo.Circuit circuit)
        {
            Console.WriteLine("CommutationCircuit");
            var bki = DataManager.Singleton.BKIDevice;

            var groups = circuit.Reles.GroupBy(r => r.Board, r => r.Number, (board, numbers) => new { board, numbers });
            foreach (var group in groups)
            {
                Console.Write($"Board: {group.board}; rele: ");
                foreach (int n in group.numbers) Console.Write($" {n},");
                Console.WriteLine();
                if (!await bki.CommutationAsync(group.board, group.numbers)) return await Task.FromResult(false);
            }

            return await Task.FromResult(true);
        }


    }
}
