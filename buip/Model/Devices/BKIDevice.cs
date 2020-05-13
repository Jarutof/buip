using buip.Shared;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace buip.Model.Devices
{
    public class BKIDevice : Device<ComPort.StatusEnum, BKICommand<ComPort.StatusEnum>>
    {
        private TaskCompletionSource<bool> statusCompletationSource;
        public byte Diagnostic { get; private set; }
        public byte Status { get; private set; }
        public UInt16 Measure { get; private set; }
        public byte[] Codes { get; private set; } = new byte[5];

        public BKIDevice(IExchangeable<ComPort.StatusEnum> ex) : base(ex) { }
        public override async Task<bool> InitAsync()
        {
            Console.WriteLine("BKIDevice Init");
            var data = await SendCommandAsync(BKICommand<ComPort.StatusEnum>.Init);
            if (data.Status != ComPort.StatusEnum.Ok) return await Task.FromResult(false);
            Console.WriteLine("BKIDevice Init finish");

            return await Task.FromResult(true);
        }
        public override async Task<bool> CheckAsync()
        {
            Console.WriteLine("CHECK START bki");

            if (!await InitAsync()) return await Task.FromResult(false);

            for (int i = 0; i < 20; i++)
            {
                int board = i / 4;
                int section = i % 4;

                Console.WriteLine($"{i} - board:{board } section:{section} ");

                var data = await SendCommandAsync(BKICommand<ComPort.StatusEnum>.Commutation(board, section, 0xFFFF));
                if (data.Status != ComPort.StatusEnum.Ok)
                {
                    Console.WriteLine("bki CheckRele error");
                    return await Task.FromResult(false);
                }
            }

            if (!await NewStatusAsync()) return await Task.FromResult(false);

            if ((await SendCommandAsync(BKICommand<ComPort.StatusEnum>.Init)).Status != ComPort.StatusEnum.Ok) return await Task.FromResult(false);

            if (!await Check6VPositiveAsync()) return await Task.FromResult(false);
            if (!await Check6VNegativeAsync()) return await Task.FromResult(false);

            bool diagOk = Diagnostic == 0;
            for (int i = 0; i < 10; i++)
            {
                DataManager.Singleton.Status.Operation.Step.Value++;
                if (!await NewStatusAsync()) return await Task.FromResult(false);
                diagOk = Diagnostic == 0;
                if (!diagOk) break;
                await Task.Delay(500);
            }
            if (!await InitAsync()) return await Task.FromResult(false);
            Console.WriteLine("CHECK FINISHED bki");
            return await Task.FromResult(diagOk);
        }

        public async Task<bool> SetVoltageAsync(bool is100V, bool isMOm)
        {
            Console.WriteLine("SetVoltage");
            return (await SendCommandAsync(BKICommand<ComPort.StatusEnum>.SetVoltage(is100V, isMOm))).Status == ComPort.StatusEnum.Ok;
        }

        public async Task<bool> CommutationOnTerminalAsync()
        {
            return (await SendCommandAsync(BKICommand<ComPort.StatusEnum>.CommutationOnTerminal)).Status == ComPort.StatusEnum.Ok;
        }

        public async Task<ResistMeasureResult> GetVoltageAsync(float vmax)
        {
            if (!await StartVoltageMeasureAsync(vmax)) return await Task.FromResult(ResistMeasureResult.Failed);
            if (!await NewMeasureAsync()) return await Task.FromResult(ResistMeasureResult.Failed);
            Console.WriteLine($"GetVoltageAsync {Measure}");
            return await Task.FromResult(new ResistMeasureResult() { Measure = Measure, IsOM = (Status & 0x0F) == 0, IsOk = true });
        }
        public struct ResistMeasureResult
        {
            public bool IsOk { get; set; }
            public bool IsOM { get; set; }
            public UInt16 Measure { get; set; }
            public static ResistMeasureResult Failed => new ResistMeasureResult() { IsOk = false };
        }
        public async Task<ResistMeasureResult> GetResistAsync(bool is100V, bool isMOm, bool withDisplay = false)
        {
            if (!await StartResistMeasureAsync(is100V, isMOm, withDisplay)) return await Task.FromResult(ResistMeasureResult.Failed);
            if (!await NewMeasureAsync()) return await Task.FromResult(ResistMeasureResult.Failed);
            return await Task.FromResult(new ResistMeasureResult() { Measure = Measure, IsOM = (Status & 0x0F) == 0, IsOk = true });
        }

        private async Task<bool> NewMeasureAsync()
        {
            while (!Status.IsBit(7))
            {
                if (Diagnostic > 0)
                    Console.WriteLine($"Diagnostic {Diagnostic.ToString("X")}");
                if (!await NewStatusAsync()) return await Task.FromResult(false);
            }

            return await Task.FromResult(true);
        }
        private async Task<bool> NewStatusAsync()
        {
            statusCompletationSource = new TaskCompletionSource<bool>();
            return await statusCompletationSource.Task;
        }

        private async Task<bool> StartResistMeasureAsync(bool is100V, bool isMOm, bool withDisplay = false)
        {
            return await Task.FromResult((await SendCommandAsync(BKICommand<ComPort.StatusEnum>.StartResistMeasure(is100V, isMOm, withDisplay))).Status == ComPort.StatusEnum.Ok);
        }
        public async Task<bool> StartVoltageMeasureAsync(float vmax)
        {
            return await Task.FromResult((await SendCommandAsync(BKICommand<ComPort.StatusEnum>.StartVoltageMeasure(vmax))).Status == ComPort.StatusEnum.Ok);
        }

        private async Task<bool> Check6VPositiveAsync()
        {
            if ((await SendCommandAsync(BKICommand<ComPort.StatusEnum>.RegisterWrite(12, 3))).Status != ComPort.StatusEnum.Ok) return await Task.FromResult(false);
            if ((await SendCommandAsync(BKICommand<ComPort.StatusEnum>.RegisterWrite(13, 4))).Status != ComPort.StatusEnum.Ok) return await Task.FromResult(false);
            if ((await SendCommandAsync(BKICommand<ComPort.StatusEnum>.RegisterWrite(14, 0x60))).Status != ComPort.StatusEnum.Ok) return await Task.FromResult(false);
            if ((await SendCommandAsync(BKICommand<ComPort.StatusEnum>.RegisterWrite(15, 0))).Status != ComPort.StatusEnum.Ok) return await Task.FromResult(false);
            await Task.Delay(300);

            byte[] value = new byte[2];
            var bkiData = await SendCommandAsync(BKICommand<ComPort.StatusEnum>.RegisterRead(13));
            if (bkiData.Status != ComPort.StatusEnum.Ok) await Task.FromResult(false);
            value[0] = bkiData.Buffer[3];

            bkiData = await SendCommandAsync(BKICommand<ComPort.StatusEnum>.RegisterRead(14));
            if (bkiData.Status != ComPort.StatusEnum.Ok) await Task.FromResult(false);
            value[1] = bkiData.Buffer[3];
            Console.WriteLine($"+6V = {value.GetUInt16(0).ToFloat()}");
            return await Task.FromResult(value.GetUInt16(0).ToFloat().IsInArea(6, 1.2f));
        }

        private async Task<bool> Check6VNegativeAsync()
        {
            if ((await SendCommandAsync(BKICommand<ComPort.StatusEnum>.RegisterWrite(12, 3))).Status != ComPort.StatusEnum.Ok) return await Task.FromResult(false);
            if ((await SendCommandAsync(BKICommand<ComPort.StatusEnum>.RegisterWrite(13, 4))).Status != ComPort.StatusEnum.Ok) return await Task.FromResult(false);
            if ((await SendCommandAsync(BKICommand<ComPort.StatusEnum>.RegisterWrite(14, 0x70))).Status != ComPort.StatusEnum.Ok) return await Task.FromResult(false);
            if ((await SendCommandAsync(BKICommand<ComPort.StatusEnum>.RegisterWrite(15, 0))).Status != ComPort.StatusEnum.Ok) return await Task.FromResult(false);
            await Task.Delay(300);

            byte[] value = new byte[2];
            var bkiData = await SendCommandAsync(BKICommand<ComPort.StatusEnum>.RegisterRead(13));
            if (bkiData.Status != ComPort.StatusEnum.Ok) await Task.FromResult(false);
            value[0] = bkiData.Buffer[3];

            bkiData = await SendCommandAsync(BKICommand<ComPort.StatusEnum>.RegisterRead(14));
            if (bkiData.Status != ComPort.StatusEnum.Ok) await Task.FromResult(false);
            value[1] = bkiData.Buffer[3];

            bkiData = await SendCommandAsync(BKICommand<ComPort.StatusEnum>.RegisterRead(12));
            if (bkiData.Status != ComPort.StatusEnum.Ok) await Task.FromResult(false);
            value[0] |= bkiData.Buffer[3];
            Console.WriteLine($"-6V = {value.GetUInt16(0).ToFloat()} ");
            return await Task.FromResult(value.GetUInt16(0).ToFloat().IsInArea(-6, 1.2f));
        }

        UInt16 GetSectionMask(IEnumerable<int> source, Func<int, bool> predicate) => (UInt16)source.Where(predicate).Select(r => 1 << ((r - 1) % 16)).Aggregate(0, (p, n) => p | n);
        public async Task<bool> CommutationAsync(int board, IEnumerable<int> numbers)
        {
            Func<int, bool>[] predicats = new Func<int, bool>[4]
            {
                n => (n - 1) < 16,
                n => ((n - 1) >= 16) && ((n - 1) < 32),
                n => ((n - 1) >= 32) && ((n - 1) < 48),
                n => ((n - 1) >= 48) && ((n - 1) < 64)
            };

            for (int i = 0; i < predicats.Length; i++)
            {
                var mask = GetSectionMask(numbers, predicats[i]);
                if (mask > 0 && (await SendCommandAsync(BKICommand<ComPort.StatusEnum>.Commutation(board - 1, i, mask))).Status != ComPort.StatusEnum.Ok) return await Task.FromResult(false);
            }
            return await Task.FromResult(true);
        }


        public override void DataChangeHandler(ICommand<ComPort.StatusEnum> cmd)
        {
            switch ((BKICommand<ComPort.StatusEnum>.Command)cmd.Data.Buffer[0])
            {
                case BKICommand<ComPort.StatusEnum>.Command.Status:
                    Diagnostic = cmd.Data.Buffer[1];
                    Status = cmd.Data.Buffer[2];
                    Measure = cmd.Data.Buffer.GetUInt16(3);
                    Codes = cmd.Data.Buffer.GetRange(5, 5).ToArray();
                    for (int i = 0; i < Codes.Length; i++)
                    {
                        Codes[i] ^= 0xFF;
                    }

                    statusCompletationSource?.TrySetResult(true);

                    if (Diagnostic > 0)
                    {
                        Console.WriteLine($"Diagnostic {Diagnostic.ToString("X")}");
                        if (DataManager.Singleton.CanStopOperationIfError)
                        {
                            DataManager.Singleton.AddRecord(DataManager.RecordTypeEnum.RT_ERROR, (byte)DataManager.EventTypeEnum.ET_BV_BKI_ERR, Diagnostic);
                            DataManager.Singleton.StopOperation(DataManager.EventTypeEnum.ET_BV_BKI_ERR);
                        }
                    }
                    break;
                default:

                    break;
            }
        }

        public override void ErrorHandler(ICommand<ComPort.StatusEnum> cmd)
        {

            Console.WriteLine($"BKI error. Command:{(BKICommand<ComPort.StatusEnum>.Command)cmd.ToArray()[0]} Status:{cmd.Data.Status}");
            statusCompletationSource?.TrySetResult(false);
            if (DataManager.Singleton.CanStopOperationIfError)
            {
                DataManager.Singleton.AddRecord(DataManager.RecordTypeEnum.RT_ERROR, (byte)DataManager.EventTypeEnum.ET_BV_BKI_EX, 0);
                DataManager.Singleton.StopOperation(DataManager.EventTypeEnum.ET_BV_BKI_EX);
            }
        }
    }
}
