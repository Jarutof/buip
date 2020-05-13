using buip.Shared;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace buip.Model.Devices
{
    public class ADCDevice : Device<ISA_SerialPort.StatusEnum, ADCCommand<ISA_SerialPort.StatusEnum>>
    {
        class StatusInfo
        {
            public byte Address { get; set; }
            public CommandsEnum Function { get; set; }
            public byte SubFunction { get; set; }
        }

        private StatusInfo status;

        private TaskCompletionSource<bool> statusCompletationSource;
        private int number;
        public float[] Voltages { get; private set; } = new float[10];
        public float[] Coefficients { get; private set; } = new float[10];

        public UInt16 ChannelsMask { get; set; }
        public Dictionary<byte, byte> ChannelsConfig { get; set; } = new Dictionary<byte, byte>();
        public ADCDevice(IExchangeable<ISA_SerialPort.StatusEnum> ex) : base(ex) { }

        public ADCDevice(IExchangeable<ISA_SerialPort.StatusEnum> ex, float initCoef) : this(ex)
        {
            for (int i = 0; i < Coefficients.Length; i++) Coefficients[i] = initCoef;
        }

        public ADCDevice(IExchangeable<ISA_SerialPort.StatusEnum> ex, float initCoef, int number) : this(ex, initCoef)
        {
            this.number = number;
        }

        public override async Task<bool> CheckAsync()
        {
            return await Task.FromResult(IsConnected);
        }

        public async Task<bool> NewStatusAsync()
        {
            statusCompletationSource = new TaskCompletionSource<bool>();
            return await statusCompletationSource.Task;
        }

        public override void DataChangeHandler(ICommand<ISA_SerialPort.StatusEnum> cmd)
        {
            status = new StatusInfo() { Address = cmd.Data.Buffer[0], Function = (CommandsEnum)cmd.Data.Buffer[1], SubFunction = cmd.Data.Buffer[2] };
            switch (status.Function)
            {
                case CommandsEnum.ReadInputChannels:
                    for (int i = 0; i < Voltages.Length; i++)
                    {
                        Voltages[i] = cmd.Data.Buffer.GetInt16(i * 2 + 3) * Coefficients[i];
                    }
                    statusCompletationSource?.TrySetResult(true);
                    break;
                case CommandsEnum.Error:
                    Console.WriteLine($"ADC {number} Error Response");
                    break;
            }

        }

        public override void ErrorHandler(ICommand<ISA_SerialPort.StatusEnum> cmd)
        {
            Console.WriteLine($"ADCDevice error:  Status:{cmd.Data.Status}");
            statusCompletationSource?.TrySetResult(false);
            if (DataManager.Singleton.CanStopOperationIfError)
            {
                DataManager.Singleton.AddRecord(DataManager.RecordTypeEnum.RT_ERROR, (byte)DataManager.EventTypeEnum.ET_BV_ADC_EX, (UInt16)number);
                DataManager.Singleton.StopOperation(DataManager.EventTypeEnum.ET_BV_ADC_EX);
            }
        }

        public override async Task<bool> InitAsync()
        {

            Console.WriteLine($"ADC Init {number}");
            var data = await SendCommandAsync(ADCCommand<ISA_SerialPort.StatusEnum>.ConfigureChannels(ChannelsMask));
            Console.WriteLine($"ADC {number} Channels Configured");
            if (data.Status != ISA_SerialPort.StatusEnum.Ok) return await Task.FromResult(false);

            if (data.Buffer[1] != 0x46) return await Task.FromResult(false);

            foreach (byte bit in ChannelsMask.GetBitNumbers())
            {
                if (ChannelsConfig.ContainsKey(bit))
                {
                    if ((await SendCommandAsync(ADCCommand<ISA_SerialPort.StatusEnum>.ConfigureChannel(bit, ChannelsConfig[bit]))).Status != ISA_SerialPort.StatusEnum.Ok) return await Task.FromResult(false);
                }
            }
            Console.WriteLine($"ADC Init finish {number}");

            return await Task.FromResult(true);
        }
    }
}
