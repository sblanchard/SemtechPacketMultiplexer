using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace PacketMultiplexer.Settings
{
    public class Miner
    {
        [JsonPropertyName("gateway_ID")]
        public string GatewayId { get; set; }
        [JsonPropertyName("server_address")]
        public string Server { get; set; }
        [JsonPropertyName("serv_port_up")]
        public int PortUp { get; set; }
        [JsonPropertyName("serv_port_down")]
        public int PortDown { get; set; }
    }
}
