using buip.Shared;
using System;
using System.Threading.Tasks;

namespace buip.Model.Devices
{
    public class StabilizerDevice : Device<BoardStatusEnum, StabilizerCommand<BoardStatusEnum>>
    {
        private UInt16 codeModel;
        private UInt16 code;
        public StabilizerDevice(IExchangeable<BoardStatusEnum> exchanger) : base(exchanger)
        {

        }

        public override async Task<bool> CheckAsync()
        {
            codeModel = 0;
            Console.WriteLine("CHECK START stab");
            var data = await SendCommandAsync(StabilizerCommand<BoardStatusEnum>.Check);
            Console.WriteLine("CHECK FINISHED stab");

            return await Task.FromResult(data.Status == BoardStatusEnum.Ok);
        }

        public override void DataChangeHandler(ICommand<BoardStatusEnum> cmd)
        {

            UInt16 newCode = cmd.Data.Buffer.GetUInt16Invert(0);
            if (newCode != code)
            {
                code = newCode;
                if ((code & 0x03FF) != codeModel)
                {
                    Console.WriteLine($"Stabilizer board error ");
                    Console.WriteLine($"model: {codeModel}; fact: {code}");
                    if (DataManager.Singleton.CanStopOperationIfError)
                    {
                        DataManager.Singleton.AddRecord(DataManager.RecordTypeEnum.RT_ERROR, (byte)DataManager.EventTypeEnum.ET_BV_STAB_EX, newCode);
                        DataManager.Singleton.StopOperation(DataManager.EventTypeEnum.ET_BV_STAB_EX);
                    }
                    return;
                }
                if ((cmd.Data.Buffer[1] & 0xFC) != 0xCC)
                {
                    Console.WriteLine($"Stabilizer error");
                    Console.WriteLine($"lo:{cmd.Data.Buffer[0]} hi:{cmd.Data.Buffer[1]} Status:{cmd.Data.Status}");
                    if (DataManager.Singleton.CanStopOperationIfError)
                    {
                        DataManager.Singleton.AddRecord(DataManager.RecordTypeEnum.RT_ERROR, (byte)DataManager.EventTypeEnum.ET_BV_STAB_EX, newCode);
                        DataManager.Singleton.StopOperation(DataManager.EventTypeEnum.ET_BV_STAB_EX);
                    }
                }
            }
        }

        public override void ErrorHandler(ICommand<BoardStatusEnum> cmd)
        {
            Console.WriteLine($"Stabilizer error");

            Console.WriteLine($"lo:{cmd.ToArray()[0]} hi:{cmd.ToArray()[1]} Status:{cmd.Data.Status}");
            if (DataManager.Singleton.CanStopOperationIfError)
            {
                DataManager.Singleton.AddRecord(DataManager.RecordTypeEnum.RT_ERROR, (byte)DataManager.EventTypeEnum.ET_BV_STAB_EX, cmd.ToArray().GetUInt16(0));
                DataManager.Singleton.StopOperation(DataManager.EventTypeEnum.ET_BV_STAB_EX);
            }
        }
        public override async Task<bool> InitAsync()
        {
            return await CheckAsync();
        }

        public async Task<bool> SetCode(UInt16 code)
        {
            if (code > codeModel)
                DataManager.Singleton.Play("-f 640 -l 20 -n -f 1040 -l 20");
            else
                DataManager.Singleton.Play("-f 340 -l 20 -n -f 240 -l 20");

            codeModel = code;

            var cmd = StabilizerCommand<BoardStatusEnum>.SetCode(code);
            var data = await SendCommandAsync(cmd);
            if (data.Status != BoardStatusEnum.Ok) return await Task.FromResult(false);
            return await Task.FromResult(true);
        }
    }
}
