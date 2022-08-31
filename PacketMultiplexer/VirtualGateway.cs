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
       
        /// <summary>
        /// Received packets count
        /// </summary>
        public uint RxCount { get; set; }
        /// <summary>
        /// Sent packets count
        /// </summary>
        public uint TxCount { get; set; }

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
                packet.stat.time = string.Concat(DateTime.UtcNow.ToString("o", CultureInfo.InvariantCulture).AsSpan(0,19), " GMT");
                packet.stat.ackr = 100;
                packet.stat.rxnb = RxCount;
                packet.stat.rxok = RxCount;
                UDPSend(miner.Server, miner.PortUp, packet.ToBytes());
            }
        }

        private void OnKeepAlive(object? state)
        {
            List<byte> pullData = new()
            {
                PacketType.PULL_DATA.Ident,
                2,
                (byte)new Random().Next(byte.MaxValue),
                (byte)new Random().Next(byte.MaxValue)
            };
            byte[] macBytes = PhysicalAddress.Parse(MacAddress).GetAddressBytes();
            foreach (var mcbyte in macBytes)
            {
                pullData.Add(mcbyte);
            }

            foreach (var miner in Miners)
            {
                UDPSend(miner.Server, miner.PortDown, pullData.ToArray());
            }
        }

        public IPEndPoint ListenAddress { get; set; }
        public IPEndPoint SendToAddress { get; set; }
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

                    //pass through to original miner
                    //packet.UpdateTime();
                    //UDPSend(endpoint, packet.ToBytes());

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
                UdpClient udpServer = new(ListenAddress.Port);
                Log.Information($"Binding to port {ListenAddress.Port}");

                while (true)
                {
                    var remoteEP = new IPEndPoint(IPAddress.Any, 1680);
                    var data = udpServer.Receive(ref remoteEP);
                    string strData = Encoding.Default.GetString(data);

                    var msgType = PacketUtil.GetMessageType(data);
                    var gwMac = PacketUtil.GetGatewayId(data);
                    
                    if (data?.Length > 12)
                    {
                        var str = Encoding.Default.GetString(data.Skip(12).ToArray());
                        if (str.StartsWith("{\"rxpk\"") || str.StartsWith("{\"txpk\"") || str.StartsWith("{\"stat\""))
                        {
                            var packet = JsonConvert.DeserializeObject<Packet>(str);
                            if (packet == null) continue;
                            if (data?.Length >= 4) packet.MessageType = PacketUtil.GetMessageType(data);
                            if (data?.Length >= 4) packet.RandomToken = PacketUtil.GetRandomToken(data);
                            if (data?.Length >= 12) packet.GatewayMAC = PacketUtil.GetGatewayId(data);

                            switch (packet.MessageType.Name)
                            {
                                case "PUSH_DATA":
                                    if (packet.rxpk.Any(s => s.size == 52))
                                    { 
                                        PacketUtil.SavePacketToFile("POC", data);
                                    }
                                    HandlePushData(packet, remoteEP);
                                    UDPSend(remoteEP, new byte[] { data[0], data[1], data[2], 0x01 });
                                    RxCount += (uint)packet.rxpk.Count;
                                    break;
                                case "PULL_DATA":
                                    HandlePullData(packet, remoteEP);
                                    UDPSend(remoteEP, new byte[] { data[0], data[1], data[2], 0x04 });
                                    break;
                                case "PULL_RESP":
                                    HandlePullResp(packet, remoteEP);
                                    break;
                            }
                        }
                    }
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
                    string strData = Encoding.Default.GetString(data);

                  
                    var msgType = PacketUtil.GetMessageType(data);
                    var gwMac = PacketUtil.GetGatewayId(data);

                    if (data?.Length > 12)
                    {
                        var str = Encoding.Default.GetString(data.Skip(12).ToArray());
                        if (str.StartsWith("{\"rxpk\"") || str.StartsWith("{\"txpk\"") || str.StartsWith("{\"stat\""))
                        {
                            var packet = JsonConvert.DeserializeObject<Packet>(str);
                            if (packet == null) continue;
                            if (data?.Length >= 4) packet.MessageType = PacketUtil.GetMessageType(data);
                            if (data?.Length >= 4) packet.RandomToken = PacketUtil.GetRandomToken(data);
                            if (data?.Length >= 12) packet.GatewayMAC = PacketUtil.GetGatewayId(data);

                            switch (packet.MessageType.Name)
                            {
                                case "PUSH_DATA":
                                    if (packet.rxpk.Any(s => s.size == 52))
                                    {
                                        PacketUtil.SavePacketToFile("POC", data);
                                        Log.Information("\t POC POC POC");
                                    }
                                    HandlePushData(packet, remoteEP);

                                    UDPSend(remoteEP, new byte[] { data[0], data[1], data[2], 0x01 });
                                    RxCount += (uint)packet.rxpk.Count;
                                    break;
                                case "PULL_DATA":
                                    HandlePullData(packet, remoteEP);
                                    UDPSend(remoteEP, new byte[] { data[0], data[1], data[2], 0x04 });
                                    break;
                                case "PULL_RESP":
                                    HandlePullResp(packet, remoteEP);
                                    break;
                            }
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Log.Error(e.ToString());
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
            //if(packet.rxpk.Count == 0 && packet.stat.time != string.Empty)
            //{
            //    Packets.Enqueue(packet);
            //    Log.Information($"Enqueued STAT {packet.MessageType.Name} {packet.JsonStat} {packet.GatewayMAC}");
            //}
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

        static void UDPSend(IPEndPoint endPoint, byte[] packet)
        {
            var client = new UdpClient();            
            client.Connect(endPoint.Address,endPoint.Port);
            client.Send(packet);
        }

        static void UDPSend(string host,int port, byte[] packet)
        {
            var client = new UdpClient();            
            client.Connect(host,port);            
            client.Send(packet);
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
