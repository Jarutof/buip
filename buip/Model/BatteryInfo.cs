using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace buip.Model
{
    public class BatteryInfo
    {
        public class Header
        {
            public int Number { get; set; }
            public Header(string src)
            {
                string[] parts = src.Split(';');
                Number = int.Parse(parts[0]);
            }
        }
        public struct Rele
        {
            public Rele(string src)
            {
                var parts = src.Split(';');

                Board = int.Parse(parts[3]);

                Number = int.Parse(string.Concat(parts[4].Skip(1)));
                if (parts[4][0] == 'Б')
                    Number += 32;
            }

            public int Board { get; set; }
            public int Number { get; set; }
        }
        public class Circuit
        {
            public Circuit()
            {

            }

            public Circuit(string src)
            {
                var parts = src.Split(';');
                Number = int.Parse(parts[2]);
            }

            public static Circuit Empty { get => new Circuit(); }
            public int Number { get; set; }
            public List<Rele> Reles { get; set; } = new List<Rele>();
        }
        public class CircuitOperation : Operation
        {
            public List<Circuit> Circuits { get; set; } = new List<Circuit>();
        }
        public class Operation
        {
            public Header Header { get; set; }
        }

        public List<Operation> Operations { get; set; } = new List<Operation>();


        public BatteryInfo(int kind)
        {
            string path = $"{kind}.CSV";
            if (File.Exists(path))
            {
                List<string> lines = new List<string>(File.ReadAllLines(path, Encoding.UTF8));
                var headers = lines.Where(line => int.TryParse(line.Split(';')[0], out int r));

                foreach (var h in headers)
                {
                    Header header = new Header(h);

                    var group = lines.Skip(lines.IndexOf(h) + 1).TakeWhile(line => string.IsNullOrEmpty(line.Split(';')[1]));
                    var groupHeaders = group.Where(g => !string.IsNullOrEmpty(g.Split(';')[2]));
                    if (groupHeaders.Count() > 0)
                    {
                        CircuitOperation co = new CircuitOperation() { Header = header };

                        foreach (var gh in groupHeaders)
                        {
                            Circuit circuit = new Circuit(gh);

                            var operationParams = lines.Skip(lines.IndexOf(gh) + 1).TakeWhile(line => string.IsNullOrEmpty(line.Split(';')[2]));
                            foreach (var param in operationParams)
                            {
                                circuit.Reles.Add(new Rele(param));
                            }
                            co.Circuits.Add(circuit);
                        }
                        Operations.Add(co);
                    }
                    else
                    {
                        var operationParams = group as ICollection<string>;
                    }

                }

            }
            else
            {
                Console.WriteLine($"Fine {path} not exists");
            }
        }
    }
}
