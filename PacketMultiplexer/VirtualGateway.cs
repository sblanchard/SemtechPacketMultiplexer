using Newtonsoft.Json;
using Serilog;
using System.Globalization;
using System.Net;
using System.Net.Sockets;
using System.Text;

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
        public IPEndPoint SendToAddress { get; set; }

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
                Log.Information($"Binding to port {ListenAddress.Port}");

                while (true)
                {
                    var remoteEP = new IPEndPoint(IPAddress.Any, 1700);
                    var data = udpServer.Receive(ref remoteEP);

                    string strData = Encoding.Default.GetString(data);
                    Packet? packet = new();

                    isPoC = false;

                    if (data?.Length > 12)
                    {
                        var str = Encoding.Default.GetString(data.Skip(12).ToArray());
                        if (str.StartsWith("{\"rxpk\"") || str.StartsWith("{\"txpk\"") || str.StartsWith("{\"stat\""))
                        {
                            packet = JsonConvert.DeserializeObject<Packet>(str);
                            if (packet.rxpk.Any(s => s.size == 52))
                            {
                                isPoC = true;
                                PacketUtil.SavePacketToFile("POC", data);
                                Log.Information($"{packet.GatewayMAC} {packet.MessageType.Name} {packet.Json}");
                            }
                        }
                    }

                    if (packet != null)
                    {                        
                        if (isPoC)
                        {                             
                            packet.rxpk[0].rssi = -new Random().Next(90, 120);
                            packet.rxpk[0].rssis = packet.rxpk[0].rssi;
                            packet.rxpk[0].lsnr = -Math.Round(new Random().NextDouble() * 4, 1);
                            Log.Information($"{packet.GatewayMAC} {packet.MessageType.Name} {packet.Json}");
                            Log.Information($"Send to 192.168.1.100");
                            UDPSend("192.168.1.100", 1680, packet.ToBytes("AA:55:5A:00:00:00:33:33"));
                            
                            packet.rxpk[0].rssi = -new Random().Next(90, 120);
                            packet.rxpk[0].rssis = packet.rxpk[0].rssi;
                            packet.rxpk[0].lsnr = -Math.Round(new Random().NextDouble() * 4, 1);
                            Log.Information($"{packet.GatewayMAC} {packet.MessageType.Name} {packet.Json}");
                            Log.Information($"Send to 192.168.1.91");
                            UDPSend("192.168.1.91", 1680, packet.ToBytes("AA:55:5A:00:00:00:22:22")); 

                            packet.rxpk[0].rssi = -new Random().Next(90, 120);
                            packet.rxpk[0].rssis = packet.rxpk[0].rssi;
                            packet.rxpk[0].lsnr = -Math.Round(new Random().NextDouble() * 4, 1);
                            Log.Information($"{packet.GatewayMAC} {packet.MessageType.Name} {packet.Json}");
                            Log.Information($"Send to 192.168.1.97");
                            UDPSend("192.168.1.97", 1680, packet.ToBytes("AA:55:5A:00:00:00:11:11"));
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Log.Error(e.ToString());
            }
        }
        private void ListenProtocol()
        {
            try
            {

                UdpClient udpServer = new(ListenAddress.Port);

                while (true)
                {
                    var remoteEP = new IPEndPoint(IPAddress.Any, 1680);
                    var data = udpServer.Receive(ref remoteEP);

                    var remoteAddress = remoteEP.Address;
                    var remotePort = remoteEP.Port;


                    string strData = Encoding.Default.GetString(data);
                    UDPSend(remoteEP, data);
                    UDPSend(remoteEP.Address.ToString(),1681, data);

                    Packet? packet = new();

                    if (data?.Length > 12)
                    {
                        var str = Encoding.Default.GetString(data.Skip(12).ToArray());
                        if (str.StartsWith("{\"rxpk\"") || str.StartsWith("{\"stat\""))
                        {
                            packet = JsonConvert.DeserializeObject<Packet>(str);
                            packet.Json = str;
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

                    if (packet.rxpk.Any(s => s.size == 52))
                    {
                        isPoC = true;
                        PacketUtil.SavePacketToFile("POC", data);
                    }
                    

                    switch (packet.MessageType.Name)
                    {
                        case "PUSH_DATA":
                            //PUSH_DATA messages from gateways are used to inform the miner of received LoRa packets.
                            //Each received LoRa packet, regardless of which gateway sent the message, is FORWARDED TO ALL GATEWAYS.
                            //Since multiple gateways may receive the same message, a cache is of recent messages is kept and duplicate LoRa packets are dropped.
                            //The metadata such as gateway MAC address is modified so each miner thinks it is communicating with a unique gateway.
                            //The RSSI, SNR, and timestamp (tmst) fields are also modified to be in acceptable ranges and to ensure the timestamps are in order
                            //and increment as expected regardless of real gateway (we cant assume timestamps are synchronized if gateway doesnt have GPS).
                            PacketUtil.SavePacketToFile(packet.MessageType.Name + $"_{packet.GatewayMAC}_Stat",data);
                            Log.Information($"{packet.GatewayMAC} {packet.MessageType.Name} {packet.Json}");
                            if (packet.stat != null && packet.stat.time != null) break;//stat msg
                            UDPSend("192.168.1.92", 1680, data);
                            //foreach (var gateway in MacAddresses)
                            //{
                            //    packet.IncrementMAC();
                            //    packet.RandomiseSignal();
                            //    packet.UpdateTime();
                            //    //var bytes = packet.ToBytes();
                            //    //UDPSend(gateway.Value, packet.ToBytes());
                            //}

                            break;
                        case "PUSH_ACK":                            
                            if (Log.IsEnabled(Serilog.Events.LogEventLevel.Information)) Log.Information($"{packet.GatewayMAC} {packet.MessageType.Name}");
                            //UDPSend("192.168.1.100", 1680, data);
                            //do nothing
                            break;
                        case "PULL_DATA":                            
                            Log.Information($"{packet.GatewayMAC} {packet.MessageType.Name} {packet.Json}"); 
                            PacketUtil.SavePacketToFile(packet.MessageType.Name, data);
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
                            Log.Information($"{packet.GatewayMAC} {packet.MessageType.Name} {packet.Json}");
                            PacketUtil.SavePacketToFile(packet.MessageType.Name + $"_{packet.GatewayMAC}", data);
                            UDPSend("192.168.1.92", 1680, data);

                            //PULL_RESP messages received from miners contain data to transmit (usually for device JOINs or PoC).
                            //These are FORWARDED UNMODIFIED TO THE GATEWAY WITH THE SAME MAC ADDRESS AS THE VIRTUAL GATEWAY INTERFACING WITH THE MINER,
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
            client.Connect(endPoint.Address,1680);
            client.Send(packet);
        }

        static void UDPSend(string host,int port, byte[] packet)
        {
            var client = new UdpClient();            
            client.Connect(host,port);            
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
