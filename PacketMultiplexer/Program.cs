using PacketMultiplexer;
using System.Net.NetworkInformation;
using System.Net;
using System.Text.Json;
using PacketMultiplexer.Settings;

namespace ler_Manager
{
    class Program
    {

        static void Main(string[] args)
        {
            //DisplayDirectedBroadcastAddresses();

            string fileName = @".\Settings\config.json";
            string jsonString = File.ReadAllText(fileName);
            var configs = JsonSerializer.Deserialize<List<Config>>(jsonString);

            //string minerFileName = "miners.json";
            //string minersjsonString = File.ReadAllText(minerFileName);
            //List<Miner> miners = JsonSerializer.Deserialize<List<Miner>>(minersjsonString);

            VirtualGatewayCollection gatewayCollection = new VirtualGatewayCollection(configs);

          

            Console.ReadLine();

        }
    }
}