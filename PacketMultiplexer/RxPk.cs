using Newtonsoft.Json;
using System.Text;
using System.Text.Json.Nodes;

namespace PacketMultiplexer
{
    // {"rxpk":[{
    //1 "jver":1,
    //2 "tmst":2767789124,
    //3"chan":4,
    //4 "rfch":0,
    //5 "freq":867.300000,
    //6 "mid":0,
    //7 "stat":1,
    //8 "modu":"LORA",
    //9 "datr":"SF12BW125",
    //10 "codr":"4/5",
    //11 "rssis":-53,
    //12 "lsnr":12.2,
    //13 "foff":0,
    //14 "rssi":-53,
    //15 "size":52,
    //16 "data":"QDDaAAFzkJFVAPJvAGpRL2smwKXs2tdv+VMD5MF2Nl+4RXZiXe3aH8v177O5lMVI17hDZw=="}]}
    [JsonObject(Title = "rxpk")]
    public class RxPk : IMessage
    { 
        [JsonProperty(Order = 1)]
        public int jver { get; set; }
        [JsonProperty(Order = 2)]
        public decimal tmst { get; set; }
        [JsonProperty(Order = 3)]
        public int chan { get; set; }
        [JsonProperty(Order = 4)]
        public int rfch { get; set; }
        [JsonProperty(Order = 5)]
        public double freq { get; set; }
        [JsonProperty(Order = 6)]
        public int mid { get; set; }
        [JsonProperty(Order = 7)]
        public int stat { get; set; }
        [JsonProperty(Order = 8)]
        public string modu { get; set; }
        [JsonProperty(Order = 9)]
        public string datr { get; set; }
        [JsonProperty(Order = 10)]
        public string codr { get; set; }
        [JsonProperty(Order = 11)]
        public int rssis { get; set; }
        [JsonProperty(Order = 12)]
        public double lsnr { get; set; }
        [JsonProperty(Order = 13)]
        public int foff { get; set; }
        [JsonProperty(Order = 14)]
        public int rssi { get; set; }
        [JsonProperty(Order = 15)]
        public int size { get; set; }
        [JsonProperty(Order = 16)]
        public string data { get; set; } 
    }
}
