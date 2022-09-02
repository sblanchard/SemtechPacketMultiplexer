using Newtonsoft.Json;
using System.Formats.Asn1;
using System.Text;
using System.Text.Json.Nodes;

namespace PacketMultiplexer.Packets
{
    /*rxpk
   Name |  Type  | Function
   :----:|:------:|--------------------------------------------------------------
    time | string | UTC time of pkt RX, us precision, ISO 8601 'compact' format
    tmms | number | GPS time of pkt RX, number of milliseconds since 06.Jan.1980
    tmst | number | Internal timestamp of "RX finished" event (32b unsigned)
    freq | number | RX central frequency in MHz (unsigned float, Hz precision)
    chan | number | Concentrator "IF" channel used for RX (unsigned integer)
    rfch | number | Concentrator "RF chain" used for RX (unsigned integer)
    stat | number | CRC status: 1 = OK, -1 = fail, 0 = no CRC
    modu | string | Modulation identifier "LORA" or "FSK"
    datr | string | LoRa datarate identifier (eg. SF12BW500)
    datr | number | FSK datarate (unsigned, in bits per second)
    codr | string | LoRa ECC coding rate identifier
    rssi | number | RSSI in dBm (signed integer, 1 dB precision)
    lsnr | number | Lora SNR ratio in dB (signed float, 0.1 dB precision)
    size | number | RF packet payload size in bytes (unsigned integer)
    data | string | Base64 encoded RF packet payload, padded
    */

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
        public string time { get; set; }
        [JsonProperty(Order = 3)]
        public string tmms { get; set; }
        [JsonProperty(Order = 4)]
        public uint tmst { get; set; }
        [JsonProperty(Order = 5)]
        public int chan { get; set; }
        [JsonProperty(Order = 6)]
        public int rfch { get; set; }
        [JsonProperty(Order = 7)]
        public double freq { get; set; }
        [JsonProperty(Order = 8)]
        public int mid { get; set; }
        [JsonProperty(Order = 9)]
        public int stat { get; set; }
        [JsonProperty(Order = 10)]
        public string modu { get; set; }
        [JsonProperty(Order = 11)]
        public string datr { get; set; }
        [JsonProperty(Order = 12)]
        public string codr { get; set; }
        [JsonProperty(Order = 13)]
        public int rssis { get; set; }
        [JsonProperty(Order = 14)]
        public double lsnr { get; set; }
        [JsonProperty(Order = 15)]
        public int foff { get; set; }
        [JsonProperty(Order = 16)]
        public int rssi { get; set; }
        [JsonProperty(Order = 17)]
        public int size { get; set; }
        [JsonProperty(Order = 18)]
        public string data { get; set; }
    }
}
