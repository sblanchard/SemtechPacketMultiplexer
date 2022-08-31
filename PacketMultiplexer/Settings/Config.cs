using System.Text.Json.Serialization;

namespace PacketMultiplexer.Settings
{
    public class Config
    {
        [JsonPropertyName("gateway_ID")]
        public string GatewayId { get; set; }
        [JsonPropertyName("server_address")]
        public string Server { get; set; }
        [JsonPropertyName("serv_port_up")]
        public int PortUp { get; set; }
        [JsonPropertyName("serv_port_down")]
        public int PortDown { get; set; }
        [JsonPropertyName("miners")]
        public List<Miner> Miners { get; set; }
    }
}
