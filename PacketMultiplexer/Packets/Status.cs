using Newtonsoft.Json;
using System.Text.Json.Serialization;

namespace PacketMultiplexer.Packets
{
    [JsonObject(Title = "stat")]
    public class Status
    {
        public string? time { get; set; }
        public double lati { get; set; }
        [JsonPropertyName("long")]
        public double lon { get; set; }
        public double alti { get; set; }
        public uint rxnb { get; set; }
        public uint rxok { get; set; }
        public uint rxfw { get; set; }
        public double ackr { get; set; }
        public double dwnb { get; set; }
        public double txnb { get; set; }
        public string? pfrm { get; set; }
        public string? mail { get; set; }
        public string? desc { get; set; }
    }
}
