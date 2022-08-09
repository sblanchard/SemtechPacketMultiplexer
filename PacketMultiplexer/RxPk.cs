using Newtonsoft.Json;
using System.Text;
using System.Text.Json.Nodes;

namespace PacketMultiplexer
{
    [JsonObject(Title = "rxpk")]
    public class RxPk : IMessage
    {
        public string time { get; set; }
        public double tmms { get; set; }
        public double tmst { get; set; }
        public double freq { get; set; }
        public double chan { get; set; }
        public double rfch { get; set; }
        public double stat { get; set; }
        public string modu { get; set; }
        public string datr { get; set; }
        public string codr { get; set; }
        public double rssi { get; set; }
        public double lsnr { get; set; }
        public double size { get; set; }
        public string data { get; set; }

        public JsonObject ToJson()
        {
            JsonObject output = new JsonObject();

            output.Add("time", time);
            output.Add("tmst", tmst);
            output.Add("freq", freq);
            output.Add("chan", chan);
            output.Add("rfch", rfch);
            output.Add("stat", stat);
            output.Add("modu", modu);

            if (modu.Equals(Modulation.LORA.ToString()))
            {
                output.Add("codr", codr);
                output.Add("lsnr", lsnr);
            }

            output.Add("datr", datr);
            output.Add("rssi", rssi);
            output.Add("size", size);

            var bytes = Encoding.Default.GetBytes(output.ToJsonString());
            output.Add("data",Convert.ToBase64String(bytes));

            return output;
        }

    }
}
