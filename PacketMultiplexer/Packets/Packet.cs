using Newtonsoft.Json;
using System.Globalization;
using System.Net;
using System.Net.NetworkInformation;
using System.Text;

namespace PacketMultiplexer.Packets
{
    /*txpk
        Name  |  Type  | Function
        :----:|:------:|--------------------------------------------------------------
         imme | bool   | Send packet immediately (will ignore tmst & time)
         tmst | number | Send packet on a certain timestamp value (will ignore time)
         tmms | number | Send packet at a certain GPS time (GPS synchronization required)
         freq | number | TX central frequency in MHz (unsigned float, Hz precision)
         rfch | number | Concentrator "RF chain" used for TX (unsigned integer)
         powe | number | TX output power in dBm (unsigned integer, dBm precision)
         modu | string | Modulation identifier "LORA" or "FSK"
         datr | string | LoRa datarate identifier (eg. SF12BW500)
         datr | number | FSK datarate (unsigned, in bits per second)
         codr | string | LoRa ECC coding rate identifier
         fdev | number | FSK frequency deviation (unsigned integer, in Hz) 
         ipol | bool   | Lora modulation polarization inversion
         prea | number | RF preamble size (unsigned integer)
         size | number | RF packet payload size in bytes (unsigned integer)
         data | string | Base64 encoded RF packet payload, padding optional
         ncrc | bool   | If true, disable the CRC of the physical layer (optiona     
         */

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

    /*stat
      Name |  Type  | Function
    :----:|:------:|--------------------------------------------------------------
     time | string | UTC 'system' time of the gateway, ISO 8601 'expanded' format
     lati | number | GPS latitude of the gateway in degree (float, N is +)
     long | number | GPS latitude of the gateway in degree (float, E is +)
     alti | number | GPS altitude of the gateway in meter RX (integer)
     rxnb | number | Number of radio packets received (unsigned integer)
     rxok | number | Number of radio packets received with a valid PHY CRC
     rxfw | number | Number of radio packets forwarded (unsigned integer)
     ackr | number | Percentage of upstream datagrams that were acknowledged
     dwnb | number | Number of downlink datagrams received (unsigned integer)
     txnb | number | Number of packets emitted (unsigned integer)
    */


    public class Packet : IPacket
    {
        private uint tmst_offset = 0;

        public PacketType MessageType { get; set; }
        public int Protocol { get; set; }
        public byte[] RandomToken { get; set; }
        public int PushDataId { get; set; }
        public string GatewayMAC { get; set; }
        public string NewGatewayMAC { get; set; }

        public List<RxPk> rxpk { get; set; } = new();
        public List<TxPk> txpk { get; set; } = new();
        public Status stat { get; set; }
        public IPEndPoint FromEndPoint { get; set; }
        public string JsonRx => JsonConvert.SerializeObject(rxpk, Formatting.None);
        public string JsonTx => JsonConvert.SerializeObject(txpk, Formatting.None);
        public string JsonStat => JsonConvert.SerializeObject(stat, Formatting.None);
        public byte[] ToBytes()
        {
            byte[] macBytes = string.IsNullOrEmpty(NewGatewayMAC) ? PhysicalAddress.Parse(GatewayMAC.Replace(":", "")).GetAddressBytes() : PhysicalAddress.Parse(NewGatewayMAC.Replace(":", "")).GetAddressBytes();
            var json = string.Empty;

            if (rxpk.Count > 0)
            {
                json = JsonConvert.SerializeObject(rxpk, Formatting.None);
                json = "{\"rxpk\":" + json + "}";
            }
            if (txpk.Count > 0)
            {
                json = JsonConvert.SerializeObject(txpk, Formatting.None);
                json = "{\"txpk\":" + json + "}";
            }
            if (stat != null)
            {
                json = JsonConvert.SerializeObject(stat, Formatting.None);
                json = "{\"stat\":" + json + "}";
            }

            List<byte> data = new()
            {
                2,
                (byte)new Random().Next(byte.MaxValue),
                (byte)new Random().Next(byte.MaxValue),
                MessageType.Ident
            };
            data.AddRange(macBytes);
            data.AddRange(Encoding.UTF8.GetBytes(json));
            byte[]? retBytes = data.ToArray();

            return retBytes;
        }

        internal void RandomiseSignal()
        {
            foreach (var rx in rxpk)
            {
                rx.rssi = -new Random().Next(90, 119);
                rx.lsnr = -Math.Round(new Random().NextDouble() * 4, 1);
            }
        }

        internal void UpdateTime()
        {
            foreach (var rx in rxpk)
            {
                var ts = DateTime.UtcNow;
                rx.time = DateTime.UtcNow.ToString("o", CultureInfo.InvariantCulture);
                var ts_midnight = new DateTime(ts.Year, ts.Month, ts.Day, 0, 0, 0, 0);
                var elapsed_us = (ts - ts_midnight).TotalSeconds * 1e6;
                rx.tmst = (uint)elapsed_us;
            }
        }
    }
}
