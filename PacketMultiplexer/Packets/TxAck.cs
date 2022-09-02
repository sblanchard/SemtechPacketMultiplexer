using Newtonsoft.Json;

namespace PacketMultiplexer.Packets
{
    [JsonObject(Title = "txpk_ack")]
    public class TxAck
    {
        public string error { get; set; }
    }
}
