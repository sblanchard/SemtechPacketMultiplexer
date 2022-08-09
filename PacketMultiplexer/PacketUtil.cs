using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PacketMultiplexer
{
    public static class PacketUtil
    {
        public static PacketType GetMessageType(byte[] packet)
        {
            if (packet.Length < 4)
                throw new Exception("At least 4 bytes data expected");
            return PacketType.Values.Where(i => i.Ident == packet[3]).First();
        }

        public static string GetGatewayId(byte[] packet)
        {
            if (packet.Length < 12)
                throw new Exception("At least 12 bytes data expected");
            return BitConverter.ToString(packet.Skip(4).Take(8).ToArray());
        }

        public static string CreateMD5(string input)
        {
            // Use input string to calculate MD5 hash
            using (System.Security.Cryptography.MD5 md5 = System.Security.Cryptography.MD5.Create())
            {
                byte[] inputBytes = Encoding.ASCII.GetBytes(input);
                byte[] hashBytes = md5.ComputeHash(inputBytes);

                return Convert.ToHexString(hashBytes); // .NET 5 +

                // Convert the byte array to hexadecimal string prior to .NET 5
                // StringBuilder sb = new System.Text.StringBuilder();
                // for (int i = 0; i < hashBytes.Length; i++)
                // {
                //     sb.Append(hashBytes[i].ToString("X2"));
                // }
                // return sb.ToString();
            }
        }

        internal static byte[] GetRandomToken(byte[] data)
        {
            return data.Skip(1).Take(2).ToArray();

        }
    }
}
