using buip.Exchangers;
using buip.Model;
using buip.Model.Devices;
using buip.Shared;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace buip
{
    public enum KeysEnum
    {
        port_pdu,
        port_adc0,
        port_adc1,
        port_adc2,
        port_bki,
        port_rele,

        boudrate_pdu,
        boudrate_adc0,
        boudrate_adc1,
        boudrate_adc2,
        boudrate_bki,
        port_stabilizer,
    }
    class Program
    {

        private string configFileName = "config.txt";
        private Dictionary<KeysEnum, string> dictionaryConfig = new Dictionary<KeysEnum, string>();
        static void Main(string[] args)
        {
            Program p = new Program();
        }

        public Program()
        {


            if (File.Exists(configFileName))
            {
                string[] lines = File.ReadAllLines(configFileName);
                foreach (string line in lines)
                {
                    string[] parts = line.Replace(" ", string.Empty).Split(':');
                    if (!string.IsNullOrEmpty(parts[0]) && !string.IsNullOrEmpty(parts[1]))
                    {
                        if (Enum.TryParse(parts[0], out KeysEnum key))
                        {
                            if (!dictionaryConfig.ContainsKey(key))
                            {
                                dictionaryConfig.Add(key, parts[1]);
                            }
                        }
                        else
                        {
                            WriteConfigMessage();
                            return;
                        }
                    }
                }
            }
            else
            {
                Console.WriteLine($"need to create {configFileName}");
                var names = Enum.GetNames(typeof(KeysEnum)).Select(s => $"{s}:");
                File.WriteAllLines(configFileName, names);
                return;
            }


            int boudratePDU = 38400;
            int boudrateADC0 = 9600;
            int boudrateADC1 = 9600;
            int boudrateADC2 = 9600;
            int boudrateBKI = 38400;

            UInt16[] adcPortNames;
            try
            {
                var values = Enum.GetValues(typeof(KeysEnum));
                foreach (KeysEnum s in values)
                {
                    if (!dictionaryConfig.ContainsKey(s)) throw new Exception();
                }
                adcPortNames = new UInt16[] { UInt16.Parse(dictionaryConfig[KeysEnum.port_adc0]), UInt16.Parse(dictionaryConfig[KeysEnum.port_adc1]), UInt16.Parse(dictionaryConfig[KeysEnum.port_adc2]) };


                if (dictionaryConfig.ContainsKey(KeysEnum.boudrate_pdu)) boudratePDU = int.Parse(dictionaryConfig[KeysEnum.boudrate_pdu]);
                if (dictionaryConfig.ContainsKey(KeysEnum.boudrate_adc0)) boudrateADC0 = int.Parse(dictionaryConfig[KeysEnum.boudrate_adc0]);
                if (dictionaryConfig.ContainsKey(KeysEnum.boudrate_adc1)) boudrateADC1 = int.Parse(dictionaryConfig[KeysEnum.boudrate_adc1]);
                if (dictionaryConfig.ContainsKey(KeysEnum.boudrate_adc2)) boudrateADC2 = int.Parse(dictionaryConfig[KeysEnum.boudrate_adc2]);
                if (dictionaryConfig.ContainsKey(KeysEnum.boudrate_bki)) boudrateBKI = int.Parse(dictionaryConfig[KeysEnum.boudrate_bki]);
            }
            catch
            {
                WriteConfigMessage();
                return;
            }

            DataManager.Init();
            ComPortExchanger pduExchanger = new ComPortExchanger(dictionaryConfig[KeysEnum.port_pdu], boudratePDU) { BaseCommand = new PDUCommand<ComPort.StatusEnum>() };
            BkiExchanger bkiExchanger = new BkiExchanger(dictionaryConfig[KeysEnum.port_bki], boudrateBKI) { BaseCommand = BKICommand<ComPort.StatusEnum>.Status };
            AdcExchanger[] adcExchangers = new AdcExchanger[3];
            ReleExchanger releExchanger = new ReleExchanger(UInt16.Parse(dictionaryConfig[KeysEnum.port_rele])) { BaseCommand = new ReleCommand<BoardStatusEnum>() };
            StabiliserExchanger stabiliserExchanger = new StabiliserExchanger(UInt16.Parse(dictionaryConfig[KeysEnum.port_stabilizer])) { BaseCommand = new StabilizerCommand<BoardStatusEnum>() };
            int[] boudratesADC = new int[] { boudrateADC0, boudrateADC1, boudrateADC2 };

            for (int i = 0; i < adcExchangers.Length; i++)
            {
                adcExchangers[i] = new AdcExchanger(adcPortNames[i], boudratesADC[i]) { BaseCommand = ADCCommand<ISA_SerialPort.StatusEnum>.GetMeasures };
            }


            DataManager.Singleton.AddDevice(DataManager.DevicesEnum.rele, new ReleDevice(releExchanger));
            DataManager.Singleton.AddDevice(DataManager.DevicesEnum.bki, new BKIDevice(bkiExchanger));
            DataManager.Singleton.AddDevice(DataManager.DevicesEnum.stabilizer, new StabilizerDevice(stabiliserExchanger)); 

            Dictionary<byte, byte> conf_all_2_5 = new Dictionary<byte, byte>();
            Dictionary<byte, byte> conf_all_2_5_last_100 = new Dictionary<byte, byte>();
            for (byte i = 0; i < 10; i++) conf_all_2_5.Add(i, 5);
            for (byte i = 0; i < 10; i++) conf_all_2_5_last_100.Add(i, 5);
            conf_all_2_5_last_100[9] = 2;


            DataManager.Singleton.AddDevice(DataManager.DevicesEnum.adc0, new ADCDevice(adcExchangers[0], DataManager.Сoefficient_2_5V, 0) { ChannelsConfig = conf_all_2_5, ChannelsMask = 0x03FF });
            DataManager.Singleton.AddDevice(DataManager.DevicesEnum.adc1, new ADCDevice(adcExchangers[1], DataManager.Сoefficient_2_5V, 1) { ChannelsConfig = conf_all_2_5, ChannelsMask = 0x03FF });
            DataManager.Singleton.AddDevice(DataManager.DevicesEnum.adc2, new ADCDevice(adcExchangers[2], DataManager.Сoefficient_2_5V, 2) { ChannelsConfig = conf_all_2_5_last_100, ChannelsMask = 0x027F });
            DataManager.Singleton.ADC_2_Device.Coefficients[9] = DataManager.Сoefficient_100mv;


            DataManager.Singleton.OnReadyPDUAnswer += (s, e) => pduExchanger.Answer(e.Value);
            pduExchanger.OnDataReceive += (s, e) => DataManager.Singleton.SetPDUData(e.Value);
            pduExchanger.OnError += (s, e) => { /*Console.WriteLine("PDU Error"); */};

            DataManager.Singleton.EndOperation();

            stabiliserExchanger.StartRequest();
            releExchanger.StartRequest();
            bkiExchanger.StartRequest();
            pduExchanger.StartListen();
            foreach (var e in adcExchangers) e.StartRequest();


            DataManager.Singleton.Play("-f 200 -l 100 -d 100 -n -f 400 -l 100 -d 100 -n -f 300 -l 100 -d 100 -n -f 500 -l 100 -n -f 100 -l 200");
            
            Task.WhenAll(DataManager.Singleton.Devices.Select(d => d.Value.InitAsync())).Wait();
            DataManager.Singleton.Play("-f 100 -l 200 -n -f 400 -l 100");

            Console.WriteLine("Press <Escape> to exit...");

            CanselKeyAsync().Wait();


            Console.WriteLine("Waiting for exchangers to stop...");

            Task[] tasks = {
                pduExchanger.StopAsync(),
                releExchanger.StopAsync(),
                stabiliserExchanger.StopAsync(),
                bkiExchanger.StopAsync(),
                adcExchangers[0].StopAsync(),
                adcExchangers[1].StopAsync(),
                adcExchangers[2].StopAsync(),
            };
            Task.WhenAll(tasks).Wait();
        }

        public static async Task CanselKeyAsync()
        {
            TaskCompletionSource<bool> tcs = new TaskCompletionSource<bool>();
            _ = Task.Factory.StartNew(() =>
               {
                   while (Console.ReadKey().Key != ConsoleKey.Escape) ;
                   tcs.SetResult(true);
               });
            await tcs.Task;
        }

        private void Play()
        {
            try
            {
                using (Process myProcess = new Process())
                {
                    myProcess.StartInfo.FileName = "bash";
                    myProcess.StartInfo.Arguments = "/root/beeps/mario-victory.sh";
                    myProcess.Start();
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }

        }

        private void WriteConfigMessage()
        {
            Console.WriteLine($"check your {configFileName}");
            var names = Enum.GetNames(typeof(KeysEnum));
            foreach (string s in names)
            {
                Console.WriteLine($"{s} : <value>");
            }
        }
    }


}
