using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PacketMultiplexer
{
    internal class PushData : HeliumPacket
    {
        private List<Status> stats;
        private List<RxPk> rxpks;
        private string GatewayMAC { get; set; }
        
        public PushData(byte[] data) : base(data, PacketType.PUSH_DATA)
        {
            if (data?.Length >= 12) GatewayMAC = PacketUtil.GetGatewayId(data);
            
        }
    }
}
