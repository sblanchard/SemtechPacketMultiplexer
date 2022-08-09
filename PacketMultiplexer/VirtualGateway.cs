using Newtonsoft.Json;
using Serilog;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace PacketMultiplexer
{
    internal class VirtualGateway
    {
        private static readonly Dictionary<string, IPEndPoint> MacAddresses = new();
        private bool isPoC;

        /// <summary>
        /// Received packets count
        /// </summary>
        public uint RxCount { get; set; }
        /// <summary>
        /// Sent packets count
        /// </summary>
        public uint TxCount { get; set; }

        public VirtualGateway(IPEndPoint listenAddress, string macAddress)
        {
            ListenAddress = listenAddress;
            MacAddress = macAddress;


            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Debug()
                .WriteTo.Console()
                .WriteTo.File("logs/myapp.txt", rollingInterval: RollingInterval.Day)
                .CreateLogger();
        }

        public IPEndPoint ListenAddress { get; set; }
        public string MacAddress { get; set; }

        public void Start()
        {
            Listen();
        }

        private void Listen()
        {
            try
            {

                UdpClient udpServer = new(ListenAddress.Port);

                while (true)
                {
                    var remoteEP = new IPEndPoint(IPAddress.Any, 1680);
                    var data = udpServer.Receive(ref remoteEP); // listen on port 11000

                    var remoteAddress = remoteEP.Address;
                    var remotePort = remoteEP.Port;

                    string strData = Encoding.Default.GetString(data);

                    Packet? packet = new();

                    if (data?.Length > 12)
                    {
                        var str = Encoding.Default.GetString(data.Skip(12).ToArray());
                        if (str.StartsWith("{\"rxpk\"") || str.StartsWith("{\"stat\""))
                        {
                            packet = JsonConvert.DeserializeObject<Packet>(str);
                        }
                    }

                    if (packet != null)
                    {
                        if (data?.Length >= 4) packet.MessageType = PacketUtil.GetMessageType(data);
                        if (data?.Length >= 4) packet.RandomToken = PacketUtil.GetRandomToken(data);
                        if (data?.Length >= 12) packet.GatewayMAC = PacketUtil.GetGatewayId(data);
                    }

                    if (packet == null) continue;

                    if (!MacAddresses.ContainsKey(packet.GatewayMAC)) MacAddresses.Add(packet.GatewayMAC, remoteEP);
                    MacAddresses[packet.GatewayMAC] = remoteEP; //keep it up to date
                    packet.FromEndPoint = remoteEP;

                    if (packet.rxpk.Any(s => s.size == 52 && s.datr == "SF9BW125")) isPoC = true;
                    
                    switch (packet.MessageType.Name)
                    {
                        case "PUSH_DATA":
                            //PUSH_DATA messages from gateways are used to inform the miner of received LoRa packets.
                            //Each received LoRa packet, regardless of which gateway sent the message, is forwarded to all gateways.
                            //Since multiple gateways may receive the same message, a cache is of recent messages is kept and duplicate LoRa packets are dropped.
                            //The metadata such as gateway MAC address is modified so each miner thinks it is communicating with a unique gateway.
                            //The RSSI, SNR, and timestamp (tmst) fields are also modified to be in acceptable ranges and to ensure the timestamps are in order
                            //and increment as expected regardless of real gateway (we cant assume timestamps are synchronized if gateway doesnt have GPS).
                            
                            if (packet.stat != null && packet.stat.time != null) break;//stat msg

                            foreach (var gateway in MacAddresses)
                            {
                                packet.IncrementMAC();
                                packet.RandomiseSignal();
                                packet.UpdateTime();
                                var bytes = packet.ToBytes();
                                UDPSend(gateway.Value, packet);
                            }

                            break;
                        case "PUSH_ACK":
                            Log.Debug(packet.GatewayMAC + " " + packet.MessageType.Name);
                            //do nothing
                            break;
                        case "PULL_DATA":
                            //PULL_DATA messages from gateways are used to ensure a communication path through any NAT or router is open.
                            //These messages contain the MAC address (same as Gateway_ID) as well as the origin IP address and port.
                            //This mapping of gateway MAC to (IP, Port) is saved so the software knows where to send PULL_RESP messages.
                            if (MacAddresses.ContainsKey(packet.GatewayMAC))
                            {
                                MacAddresses[packet.GatewayMAC] = packet.FromEndPoint;
                            }
                            break;
                        case "PULL_ACK":
                            //do nothing
                            break;
                        case "PULL_RESP":
                            //PULL_RESP messages received from miners contain data to transmit (usually for device JOINs or PoC).
                            //These are forwarded unmodified to the gateway with the same MAC address as the virtual gateway interfacing with the miner,
                            //if it exists, and a PULL_DATA was received from the gateway.
                            //This ensures transmit behavior of a miner remains consistent.
                            //This restriction may be removed in later revisions.
                            //To ensure PULL_RESPs are received by the other miners, a fake PUSH_DATA payload is created for every PULL_RESP with simulated RSSI, SNR, and timestamp(currently hardcoded RSSI and SNR).
                            //This fake PUSH_DATA runs through the same process as real ones except it is not forwarded to the miner that sent the PULL_RESP(so gateways don't receive their own transmissions).

                            break;
                        case "TX_ACK":
                            //do nothing
                            break;
                    }
                }

            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error");
            }
            finally
            {
                Log.CloseAndFlush();
            }
        }

        static void UDPSend(IPEndPoint endPoint, byte[] packet)
        {
            var client = new UdpClient();
            IPEndPoint ep = new(endPoint.Address, 1680);
            client.Connect(endPoint);

            // send data
            client.Send(packet);
        }

        public byte[] GetStat()
        {             
            var payload = new Status
            {
                time = DateTime.UtcNow.ToString("o", CultureInfo.InvariantCulture),
                rxnb = RxCount,
                rxok = RxCount,
                rxfw = RxCount,
                txnb = TxCount,
                dwnb = TxCount,
                ackr = 100
            };
            return GetPushData(payload);
        }

        public void GetPushData(IMessage message)
        {

        }

        public byte[] GetPushData(Status message)
        {
            Dictionary<string, object> data = new();
            data.Add("Ident",PacketType.PUSH_DATA.Ident);
            data.Add("identifier", PacketType.PUSH_DATA.Ident);
            return null;
        }
    }
}
