using buip.Shared;
using System;
using System.Threading.Tasks;

namespace buip.Model.Devices
{
    public class ReleDevice : Device<BoardStatusEnum, ReleCommand<BoardStatusEnum>>
    {
        public ReleDevice(IExchangeable<BoardStatusEnum> ex) : base(ex) { }
        private byte[] state_model = new byte[4];
        private byte[] state_control = new byte[4];
        bool diagOk = true;
        private async Task<bool> WriteMask(byte addr, byte mask)
        {
            bool res = await Task.FromResult((await SendCommandAsync(new ReleCommand<BoardStatusEnum>(addr, mask))).Status == BoardStatusEnum.Ok);
            if (res)
            {
                state_control[addr] = mask;
            }

            return await Task.FromResult(res);

        }

        public override async Task<bool> InitAsync()
        {
            Console.WriteLine("ReleDevice Init");
            if (!await WriteMask(0, 0)) return await Task.FromResult(false);
            if (!await WriteMask(1, 0)) return await Task.FromResult(false);
            if (!await WriteMask(2, 0)) return await Task.FromResult(false);
            if (!await WriteMask(3, 0)) return await Task.FromResult(false);
            Console.WriteLine("ReleDevice Init finish");

            return await Task.FromResult(true);
        }

        public async Task<bool> OnAsync(int n)
        {
            byte addr = (byte)(n / 8);
            state_model[addr] = state_model[addr].SetBit(n % 8);
            return await WriteMask(addr, state_model[addr]);
        }
        public async Task<bool> OffAsync(int n)
        {
            byte addr = (byte)(n / 8);
            state_model[addr] = state_model[addr].RemBit(n % 8);
            return await WriteMask(addr, state_model[addr]);
        }

        public override async Task<bool> CheckAsync()
        {
            Console.WriteLine("CHECK START rele");
            diagOk = true;
            for (int i = 0; i < 32; i++)
            {
                if (!await OnAsync(i))
                {
                    diagOk = false;
                    break;
                }

            }

            for (int i = 0; i < 32; i++)
            {
                if (!await OffAsync(i))
                {
                    diagOk = false;
                    break;
                }
            }
            Console.WriteLine("CHECK FINISHED rele");

            return await Task.FromResult(diagOk);

        }

        public override void DataChangeHandler(ICommand<BoardStatusEnum> cmd)
        {
            for (int i = 0; i < 4; i++)
            {
                if (state_control[i] != cmd.Data.Buffer[i])
                {
                    diagOk = false;
                    Console.WriteLine($"Rele board error. {i} model:{state_control[i]}; control:{cmd.Data.Buffer[i]};");
                    if (DataManager.Singleton.CanStopOperationIfError)
                    {
                        DataManager.Singleton.AddRecord(DataManager.RecordTypeEnum.RT_ERROR, (byte)DataManager.EventTypeEnum.ET_BV_RELE_ERR, 0);
                        DataManager.Singleton.StopOperation(DataManager.EventTypeEnum.ET_BV_RELE_ERR);
                    }
                }
                state_control[i] = cmd.Data.Buffer[i];
            }
        }

        public override void ErrorHandler(ICommand<BoardStatusEnum> cmd)
        {
            Console.WriteLine($"Rele error. addr:{cmd.Data.Buffer[0]} Status:{cmd.Data.Status}");
            if (DataManager.Singleton.CanStopOperationIfError)
            {
                DataManager.Singleton.AddRecord(DataManager.RecordTypeEnum.RT_ERROR, (byte)DataManager.EventTypeEnum.ET_BV_RELE_EX, 0);
                DataManager.Singleton.StopOperation(DataManager.EventTypeEnum.ET_BV_RELE_EX);
            }
        }
    }
}
