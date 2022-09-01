using Newtonsoft.Json;
using PacketMultiplexer.Settings;
using Serilog;
using System.Collections.Concurrent;
using System.Globalization;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;
using Timer = System.Threading.Timer;
using System;
using System.Linq;

namespace PacketMultiplexer
{
    internal class VirtualGateway
    {
        private readonly Dictionary<string, IPEndPoint> MacAddresses = new();
        private ConcurrentQueue<Packet> Packets = new();
        public List<Miner> Miners { get; set; } = new List<Miner>();
        private readonly object sendLock = new object();

        /// <summary>
        /// Received packets count
        /// </summary>
        private Dictionary<string, uint> RxCount = new();
        /// <summary>
        /// Sent packets count
        /// </summary>
        private Dictionary<string, uint> TxCount = new();

        /// <summary>
        /// ctor
        /// </summary>
        /// <param name="listenAddress"></param>
        /// <param name="macAddress"></param>
        /// <param name="miners"></param>
        public VirtualGateway(IPEndPoint listenAddress, string macAddress, List<Miner> miners)
        {
            ListenAddress = listenAddress;
            MacAddress = macAddress;
            Miners = miners;

            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Debug()
                .WriteTo.Console()
                .WriteTo.File("logs/myapp.txt", rollingInterval: RollingInterval.Day)
                .CreateLogger();

           var keepAlive = new Timer(OnKeepAlive, null, 10000, 10000);
           var sendStat = new Timer(SendStat, null, 10000, 30000);

        }

        /// <summary>
        /// Send stat every 30 sec
        /// </summary>
        /// <param name="state"></param>
        private void SendStat(object? state)
        {
            foreach (var miner in Miners)
            {
                Packet packet = new()
                {
                    stat = new Status(),
                    GatewayMAC = miner.GatewayId,
                    MessageType = PacketType.PUSH_DATA
                };
                var rx = RxCount.Where(g => g.Key == miner.GatewayId).FirstOrDefault().Value;
                var tx = TxCount.Where(g => g.Key == miner.GatewayId).FirstOrDefault().Value;

                packet.stat.time = string.Concat(DateTime.UtcNow.ToString("o", CultureInfo.InvariantCulture).AsSpan(0,19), " GMT");
                packet.stat.ackr = 100;
                packet.stat.rxnb = rx;
                packet.stat.rxok = rx;
                packet.stat.rxfw = rx;
                packet.stat.txnb = tx;
                packet.stat.dwnb = tx;
                UDPSend(miner.Server, miner.PortUp, packet.ToBytes());
            }
        }

        /// <summary>
        /// Keep-alive every 10 sec
        /// </summary>
        /// <param name="state"></param>
        private void OnKeepAlive(object? state)
        {
            foreach (var miner in Miners)
            {
                Packet packet = new()
                {
                    GatewayMAC = miner.GatewayId,
                    MessageType = PacketType.PULL_DATA,
                    Protocol = 2,                    
                };

                UDPSend(miner.Server, miner.PortDown, packet.ToBytes());
            }
        }

        public IPEndPoint ListenAddress { get; set; }        
        public string MacAddress { get; set; }

        public void Start()
        {
            UdpClient[] minerClients = new UdpClient[Miners.Count];
            Task.Run(ListenGateway);
            Log.Information($"Gateway {MacAddress} Start");
            Thread.Sleep(1000);

            for (int i = 0; i <= minerClients.Length-1; i++)
            {
                var client = minerClients[i];
                client = new(Miners[i].PortUp);
                Log.Information($"Miner {Miners[i].GatewayId} Start"); 
                Task.Run(() => ListenMiner(client));
            }

            Task.Run(HandlePacketQueue);
            Log.Information("Packet Queue Processor Start");
        }

        private void HandlePacketQueue()
        {
            while(true)
            {
                if (Packets.TryDequeue(out var packet))
                {
                    if (!MacAddresses.ContainsKey(packet.GatewayMAC)) continue;

                    //send poc to all
                    if (packet.MessageType == PacketType.PUSH_DATA)
                    {
                        foreach (var miner in Miners)
                        {
                            packet.NewGatewayMAC = miner.GatewayId;
                            packet.UpdateTime();
                            UDPSend(miner.Server, miner.PortUp, packet.ToBytes());
                        }
                    }

                    if (packet.MessageType == PacketType.PULL_RESP)
                    {
                        foreach (var miner in Miners)
                        {
                            var endpoint = MacAddresses[miner.GatewayId];

                            packet.NewGatewayMAC = miner.GatewayId;
                            packet.UpdateTime();
                            UDPSend(endpoint, packet.ToBytes());
                        }
                    }
                }
                Thread.Sleep(10);
            }
        }

        private void ListenGateway()
        {
            try
            {
                UdpClient udpClient = new(ListenAddress.Port);                
                Log.Information($"Binding to port {ListenAddress.Port}");

                while (true)
                {
                    var remoteEP = new IPEndPoint(IPAddress.Any, 1680);
                    var data = udpClient.Receive(ref remoteEP);
                    HandleProtocolMessage(data, udpClient, remoteEP);
                }
            }
            catch (Exception e)
            {
                Log.Error(e,"Error");
            }
        }

        private void ListenMiner(UdpClient udpClient)
        {
            try
            {
                while (true)
                {
                    var remoteEP = new IPEndPoint(IPAddress.Any, 1680);
                    var data = udpClient.Receive(ref remoteEP);
                    HandleProtocolMessage(data, udpClient, remoteEP);
                }
            }
            catch (Exception e)
            {
                Log.Error(e.ToString());
            }
        }

        private void HandleProtocolMessage(byte[] data,UdpClient udpClient, IPEndPoint remoteEP)
        {
            var msgType = PacketUtil.GetMessageType(data);
            var gwMac = PacketUtil.GetGatewayId(data);

            if (data?.Length >= 12)
            {
                Packet packet = new();
                var str = Encoding.Default.GetString(data.Skip(12).ToArray());
                if (str.StartsWith("{\"rxpk\"") || str.StartsWith("{\"txpk\"") || str.StartsWith("{\"stat\""))
                {
                    packet = JsonConvert.DeserializeObject<Packet>(str);
                    if (packet == null) return;
                    if (data?.Length >= 4) packet.MessageType = PacketUtil.GetMessageType(data);
                    if (data?.Length >= 4) packet.RandomToken = PacketUtil.GetRandomToken(data);
                    if (data?.Length >= 12) packet.GatewayMAC = PacketUtil.GetGatewayId(data);
                }
                else
                {
                    packet.MessageType = msgType;
                    packet.GatewayMAC = gwMac;
                }
                switch (msgType.Name)
                {
                    case "PUSH_DATA":
                        udpClient.Send(new byte[] { data[0], data[1], data[2], 0x01 }, 4, remoteEP);//ACK
                        if (packet.rxpk.Any(s => s.size == 52))
                        {
                            HandlePushData(packet, remoteEP);
                            PacketUtil.SavePacketToFile("POC", data);
                            Log.Information("\t POC POC POC");
                        }
                        if (!RxCount.ContainsKey(gwMac)) RxCount.Add(gwMac, 0);
                        RxCount[gwMac] += (uint)packet.rxpk.Count;
                        break;
                    case "PULL_DATA":
                        udpClient.Send(new byte[] { data[0], data[1], data[2], 0x04 }, 4, remoteEP);//ACK
                        HandlePullData(packet, remoteEP);
                        if (!TxCount.ContainsKey(gwMac)) TxCount.Add(gwMac, 0);
                        TxCount[gwMac] += (uint)packet.txpk.Count;
                        break;
                    case "PULL_RESP":
                        HandlePullResp(packet, remoteEP);
                        break;
                }
            }
        }

        private void HandlePushData(Packet packet, IPEndPoint endPoint)
        {
            if (!MacAddresses.ContainsKey(packet.GatewayMAC))
            {
                MacAddresses.Add(packet.GatewayMAC, endPoint);
                Log.Information($"Discovered Gateway MAC: {packet.GatewayMAC} at {endPoint.Address}:{endPoint.Port}");
            }
            MacAddresses[packet.GatewayMAC] = endPoint;

            if (packet.rxpk.Count > 0)
            {
                Log.Information($"{packet.GatewayMAC} {packet.MessageType.Name} {packet.JsonRx}");
                foreach (var miner in Miners)
                {
                    packet.RandomiseSignal();
                    packet.UpdateTime();
                    packet.NewGatewayMAC = miner.GatewayId;
                    Packets.Enqueue(packet);
                    Log.Information($"Enqueued {packet.MessageType.Name} {packet.JsonRx} {packet.NewGatewayMAC}");
                }
            }
            if (packet.rxpk.Count == 0 && packet.stat.time != string.Empty)
            {
                Packets.Enqueue(packet);
                Log.Information($"Enqueued STAT {packet.MessageType.Name} {packet.JsonStat} {packet.GatewayMAC}");
            }
        }

        private void HandlePullData(Packet packet, IPEndPoint endPoint)
        {
            if (!MacAddresses.ContainsKey(packet.GatewayMAC))
            {
                MacAddresses.Add(packet.GatewayMAC, endPoint);
                Log.Information($"Discovered Gateway MAC: {packet.GatewayMAC} at {endPoint.Address}:{endPoint.Port}");
            }
            MacAddresses[packet.GatewayMAC] = endPoint;
        }

        private void HandlePullResp(Packet packet,IPEndPoint endPoint)
        {
            Log.Information($"Handle PULL_RESP from {packet.GatewayMAC} at {endPoint.Address}:{endPoint.Port}");
            
            var tx = packet.txpk;
            tx[0].powe -= 10;
            Packets.Enqueue(packet);
        }

        private void UDPSend(IPEndPoint endPoint, byte[] packet)
        {
            lock (sendLock)
            {
                var client = new UdpClient();
                client.Connect(endPoint.Address, endPoint.Port);
                client.Send(packet, packet.Length);
            }
        }

        private void UDPSend(string host,int port, byte[] packet)
        {
            lock (sendLock)
            {
                var client = new UdpClient();
                client.Connect(host, port);
                client.Send(packet, packet.Length);
            }
        }         
    }
}
