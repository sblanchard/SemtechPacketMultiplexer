using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace PacketMultiplexer
{
    [StructLayout(LayoutKind.Sequential)]
    internal struct PacketStruct
    {
        public PacketStruct(uint random, byte packetType, string mac, string json)
        {
            RandomToken = random;
            PacketType = packetType;
            GatewayMAC = mac;
            Json = json;
        }

        public byte ProtocolVersion = 0x02;
        public uint RandomToken { get; set; }
        public byte PacketType { get; set; }
        public string GatewayMAC { get; set; }
        public string? Json { get; set; }
    }
}
