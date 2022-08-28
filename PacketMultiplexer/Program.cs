using PacketMultiplexer;
using System.Text.Json;

namespace ler_Manager
{
    class Program
    {
        private readonly static List<VirtualGateway> VirtualGateways = new();


        static void Main(string[] args)
        {
            string fileName = "config.json";
            string jsonString = File.ReadAllText(fileName);
            Config config = JsonSerializer.Deserialize<Config>(jsonString);

            VirtualGateways.Add
                (new VirtualGateway
                (new System.Net.IPEndPoint
                (System.Net.IPAddress.Parse(config.Server),
                config.PortDown),
                config.GatewayId));

            foreach (var gateway in VirtualGateways)
            {
                gateway.Start();
            }

            Console.ReadLine();

        }
    }
}