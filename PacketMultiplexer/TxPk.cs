using Newtonsoft.Json;

namespace PacketMultiplexer
{
    [JsonObject(Title = "txpk")]
    public class TxPk : IMessage
    {
        public bool imme { get; set; }
        public double tmst { get; set; }
        public double tmms { get; set; }
        public ulong freq { get; set; }
        public int rfch { get; set; }
        public double powe { get; set; }
        public string modu { get; set; }
        public string datr { get; set; }
        public string codr { get; set; }
        public uint fdev { get; set; }
        public bool ipol { get; set; }
        public uint prea { get; set; }
        public uint size { get; set; }
        public string data { get; set; }
        public bool ncrc { get; set; }
    }
}
