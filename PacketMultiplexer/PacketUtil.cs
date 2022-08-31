using System.Net.NetworkInformation;
using System.Runtime.CompilerServices;
using System.Text;

namespace PacketMultiplexer
{
    public static class PacketUtil
    {
        public static PacketType GetMessageType(byte[] packet)
        {
            if (packet.Length < 4)
                throw new Exception("At least 4 bytes data expected");
            return PacketType.Values.Where(i => i.Ident == packet[3]).FirstOrDefault();
        }

        public static string GetGatewayId(byte[] packet)
        {
            if (packet.Length < 12)
                throw new Exception("At least 12 bytes data expected");
            return BitConverter.ToString(packet.Skip(4).Take(8).ToArray()).Replace("-", ":");
        }

        public static byte[] SetGatewayId(byte[] packet, string mac)
        {            
            var bytes = PhysicalAddress.Parse(mac).GetAddressBytes();

            for (int i = 4; i <= 11; i++)
            {
                packet[i] = bytes[i - 4];
            }
            return packet;
        }

        public static string CreateMD5(string input)
        {            
            using (System.Security.Cryptography.MD5 md5 = System.Security.Cryptography.MD5.Create())
            {
                byte[] inputBytes = Encoding.ASCII.GetBytes(input);
                byte[] hashBytes = md5.ComputeHash(inputBytes);

                return Convert.ToHexString(hashBytes); 
            }
        }

        internal static byte[] GetRandomToken(byte[] data)
        {
            return data.Skip(1).Take(2).ToArray();
        }

        internal static byte[] SetRandomToken(byte[] data)
        {
            data[1] = (byte)new Random().Next(254);
            data[2] = (byte)new Random().Next(254);            
            return data;
        }

        public static unsafe byte[] SerializeValueType<T>(in T value) where T : unmanaged
        {
            byte[] result = new byte[sizeof(T)];
            Unsafe.As<byte, T>(ref result[0]) = value;
            return result;
        }

        // Note: Validation is omitted for simplicity
        public static T DeserializeValueType<T>(byte[] data) where T : unmanaged
        {
            return Unsafe.As<byte, T>(ref data[0]);
        }

        public static void SavePacketToFile(string messageType, byte[] data)
        {
            File.WriteAllBytes($"./packetlog/packet_{messageType}_{DateTime.Now.Ticks}.bin", data);
        }
    }
}
