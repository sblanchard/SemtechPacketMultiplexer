using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PacketMultiplexer
{
    public abstract class HeliumPacket
    {
        private byte version;
        private readonly byte[] packetbytes;
        private readonly PacketType identifier;

        public HeliumPacket(byte[] data, PacketType packetType)
        {
            packetbytes = data;
            identifier = packetType;
        }

        public void ToRaw(byte[] bytes)
        {
            bytes.Append((byte)0x02);
            foreach(var b in packetbytes) { bytes.Append(b); }            
            bytes.Append(identifier.Ident);
        }
    };
}
}
