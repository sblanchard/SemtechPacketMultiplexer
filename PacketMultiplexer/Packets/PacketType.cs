namespace PacketMultiplexer.Packets
{
    public class PacketType
    {
        public static readonly PacketType PUSH_DATA = new("PUSH_DATA", 0x00);
        public static readonly PacketType PUSH_ACK = new("PUSH_ACK", 0x01);
        public static readonly PacketType PULL_DATA = new("PULL_DATA", 0x02);
        public static readonly PacketType PULL_ACK = new("PULL_ACK", 0x04);
        public static readonly PacketType PULL_RESP = new("PULL_RESP", 0x03);
        public static readonly PacketType TX_ACK = new("TX_ACK", 0x05);

        public static IEnumerable<PacketType> Values
        {
            get
            {
                yield return PUSH_DATA;
                yield return PUSH_ACK;
                yield return PULL_DATA;
                yield return PULL_ACK;
                yield return PULL_RESP;
                yield return TX_ACK;
            }
        }

        public string Name { get; set; }
        public byte Ident { get; set; }
        PacketType(string name, byte ident) => (Name, Ident) = (name, ident);
    }
}
