﻿using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace PacketMultiplexer
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
        public PacketType MessageType { get; set; }
        public int Protocol { get; set; }
        public byte[] RandomToken { get; set; }
        public int PushDataId { get; set; }
        public string GatewayMAC { get; set; }
        public List<RxPk> rxpk { get; set; } = new();
        public List<TxPk> txpk { get; set; } = new();
        public Status stat { get; set; }
        public IPEndPoint FromEndPoint { get; set; }

        public Packet()
        {

        }

        public void IncrementMAC()
        {
            //D8-BB-C1-91-99-3F
            var last = GatewayMAC.Substring(14, 2);
            var first = GatewayMAC[..14];
            var numLast = int.Parse(last);
            numLast++;
            if (numLast < 10)
            {
                GatewayMAC = first + $"0{numLast}";
            }
            else
            {
                GatewayMAC = first + $"{numLast}";
            }
        }

        public Packet(byte[] data)
        {
            if (data?.Length >= 4) MessageType = PacketUtil.GetMessageType(data);
            if (data?.Length >= 12) GatewayMAC = PacketUtil.GetGatewayId(data);

            if (data?.Length > 12)
            {
                var strData = Encoding.Default.GetString(data.Skip(12).ToArray());
                if (strData.StartsWith("{\"rxpk\"") || strData.StartsWith("{\"stat\""))
                {
                    var pkt = JsonConvert.DeserializeObject<Packet>(strData);
                }
            }
        }

        public byte[] ToBytes()
        {
            byte[] retBytes = null;

            byte[] macBytes = GatewayMAC.Split(':').Select(x => Convert.ToByte(x, 16)).ToArray();
             
            if (MessageType.Name == PacketType.PUSH_DATA.Name)
            {
                var json = JsonConvert.SerializeObject(rxpk);
                var fields = new object[] { 2, new Random().Next(int.MaxValue), 0x00, macBytes, json };
                retBytes = StructConverter.Pack(fields);
            }
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
                rx.time = DateTime.UtcNow.ToString("o", CultureInfo.InvariantCulture);
            }
        }
    }

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

    [JsonObject(Title = "txpk_ack")]
    public class TxAck
    {
       public string error { get; set; }
    }
}