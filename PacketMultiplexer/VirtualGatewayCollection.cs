using PacketMultiplexer.Settings;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace PacketMultiplexer
{


    public class VirtualGatewayCollection
    {

        private readonly static List<VirtualGateway> VirtualGateways = new();

        public VirtualGatewayCollection(List<Config> configs)
        {

            foreach (var config in configs)
            {
                VirtualGateways.Add(new VirtualGateway
                                    (new IPEndPoint
                                    (IPAddress.Parse(config.Server), config.PortDown), 
                                    config.GatewayId, 
                                    config.Miners));
            }

           

            Thread.Sleep(333);

            foreach (var gateway in VirtualGateways)
            {
                gateway.Start();
            }
        }
    }
}
