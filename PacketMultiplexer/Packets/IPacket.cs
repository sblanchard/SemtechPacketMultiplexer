using System.Net;

namespace PacketMultiplexer.Packets
{
    public interface IPacket
    {
        IPEndPoint FromEndPoint { get; set; }
        string GatewayMAC { get; set; }
        PacketType MessageType { get; set; }
    }
}