using System.Text.Json;
using PacketMultiplexer;
using System.Text;

namespace PacketInspector
{
    public partial class frmMain : Form
    {
        string path = "C:\\DEV\\PacketMultiplexer\\PacketMultiplexer\\PacketMultiplexer\\bin\\Debug\\net6.0\\packetlog";

        public frmMain()
        {
            InitializeComponent();
        }

        private void frmMain_Load(object sender, EventArgs e)
        {
            LoadList();
        }

        private void LoadList()
        {
            var files = Directory.GetFiles(path);
            foreach (var file in files)
            {
                lstPacketFiles.Items.Add(Path.GetFileName(file));
            }
        }

        private void lstPacketFiles_SelectedIndexChanged(object sender, EventArgs e)
        {
            ReadFile(lstPacketFiles.Text);
        }

        private void ReadFile(string file)
        {
            var data =File.ReadAllBytes(Path.Combine(path, file));
            if (data?.Length >= 12) txtMac.Text = PacketUtil.GetGatewayId(data); 
            if (data?.Length >= 4) lblMessageType.Text = PacketUtil.GetMessageType(data).Name;
            if (data?.Length > 12)
            {
                var strData = Encoding.Default.GetString(data.Skip(12).ToArray());
                if (strData.StartsWith("{\"rxpk\"") || strData.StartsWith("{\"stat\""))
                {
                    var pkt = JsonSerializer.Deserialize<Packet>(strData);
                    pkt.MessageType = PacketUtil.GetMessageType(data);
                    pkt.GatewayMAC = PacketUtil.GetGatewayId(data);
                    var rx = pkt.rxpk[0];
                    if(rx != null)
                    {
                        rx.rssi = -99;
                        rx.lsnr = -2;
                    }
                    var bytes = pkt.ToBytes();
                    DecodeMyEncoding(bytes);
                }
                txtDecoded.Text = strData;
            }
        }

        private void DecodeMyEncoding(byte[] data)
        {            
            if (data?.Length >= 12) txtMac.Text = PacketUtil.GetGatewayId(data);
            if (data?.Length >= 4) lblMessageType.Text = PacketUtil.GetMessageType(data).Name;
            if (data?.Length > 12)
            {
                var strData = Encoding.Default.GetString(data.Skip(12).ToArray());
      //          strData = "{\"rxpk\":" + strData + "}";

                if (strData.StartsWith("{\"rxpk\"") || strData.StartsWith("{\"stat\""))
                {
                    var pkt = JsonSerializer.Deserialize<Packet>(strData);
                    pkt.MessageType = PacketUtil.GetMessageType(data);
                    pkt.GatewayMAC = PacketUtil.GetGatewayId(data);
                    var rx = pkt.rxpk[0];
                    if (rx != null)
                    {
                        rx.rssi = -99;
                        rx.lsnr = -2;
                    }
                    var bytes = pkt.ToBytes();

                }
                txtJson.Text = strData;
            }
        }

        private void btnDeserialize_Click(object sender, EventArgs e)
        {
            var data = File.ReadAllBytes(Path.Combine(path, lstPacketFiles.Text));
            if (data?.Length >= 12) txtMac.Text = PacketUtil.GetGatewayId(data);
            if (data?.Length >= 4) lblMessageType.Text = PacketUtil.GetMessageType(data).Name;
            if (data?.Length > 12)
            {
                var strData = Encoding.Default.GetString(data.Skip(12).ToArray());
                if (strData.StartsWith("{\"rxpk\"") || strData.StartsWith("{\"stat\""))
                {
                    var pkt = JsonSerializer.Deserialize<Packet>(strData);
                    txtDecoded.Text += Environment.NewLine + "OK";
                } 
            }
        }
    }
}