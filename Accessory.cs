using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using HomeKit.Mdns;
using HomeKit.Resources;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using QRCoder;

namespace HomeKit
{
    public class Accessory : IDisposable
    {
        private readonly ILoggerFactory m_LoggerFactory;
        private readonly ILogger m_Logger;

        public string Name { get; }
        public string PinCode { get; }
        public Category Category { get; }
        public string SetupId { get; }
        public string MacAddress { get; }
        public bool Paired { get; }
        public ushort Port { get; }
        public IPAddress IpAddress { get; }

        /// <param name="pinCode">Format: 000-00-000</param>
        public Accessory(string name, string pinCode, Category category, ILoggerFactory? loggerFactory)
        {
            Name = name;
            PinCode = pinCode;
            Category = category;
            SetupId = Utils.GenerateSetupId();
            MacAddress = Utils.GenerateMacAddress();
            Paired = false;
            Port = 23232;
            IpAddress = IPAddress.Parse("192.168.1.101");

            loggerFactory ??= new NullLoggerFactory();
            m_LoggerFactory = loggerFactory;
            m_Logger = loggerFactory.CreateLogger($"Accessory<{name}>");

            // todo
            //AddAccessoryInformationService();
        }

        public void Publish()
        {
            if (!Paired)
            {
                PrintSetupMessage();
            }

            m_Logger.LogTrace("MAC address: {mac}", MacAddress);
            m_Logger.LogTrace("Port: {port}", Port);
            m_Logger.LogTrace("Address: {address}", IpAddress);

            var listener = new TcpListener(IPAddress.Any, Port);
            Task.Run(() => TcpLobbyTask(listener));

            var nis = Utils.GetMulticastNetworkInterfaces();
            var ni = nis.FirstOrDefault(x => x.GetIPProperties().UnicastAddresses.Any(y => y.Address.Equals(IpAddress)));
            if (ni is null)
            {
                m_Logger.LogError("Failed to find network interface with specified address");
                return;
            }

            m_Logger.LogTrace("Interface: {interface}", ni.Name);

            var adapterIndex = ni.GetIPProperties().GetIPv4Properties().Index;
            var multicastInterface = IPAddress.HostToNetworkOrder(adapterIndex);
            var membership = new MulticastOption(IPAddress.Parse(Const.MdnsMulticastAddress), adapterIndex);

            var udpClient = new UdpClient();
            udpClient.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
            udpClient.Client.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.MulticastInterface, multicastInterface);
            udpClient.Client.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.AddMembership, membership);
            udpClient.Client.Bind(new IPEndPoint(IPAddress.Any, Const.MdnsPort));

            Task.Run(() => UdpTask(udpClient));

        }

        private async Task TcpLobbyTask(TcpListener listener)
        {
            listener.Start();

            while (true)
            {
                m_Logger.LogInformation("Waiting for TCP client...");

                var client = await listener.AcceptTcpClientAsync();

                m_Logger.LogInformation("Accepted new TCP client {remote}", client.Client.RemoteEndPoint);

                _ = Task.Run(() => TcpTask(client));
            }
        }

        private async Task TcpTask(TcpClient client)
        {
            var buffer = new byte[2048];
            var stream = client.GetStream();

            while (client.Connected)
            {
                var length = await stream.ReadAsync(buffer);
                if (length == 0)
                {
                    break;
                }

                m_Logger.LogInformation("Received TCP data {length}", length);
                m_Logger.LogTrace("TCP: {data}", BitConverter.ToString(buffer[0..length]));
            }

            m_Logger.LogInformation("Closing TCP client {remote}", client.Client.RemoteEndPoint);
            client.Close();
        }

        private async Task UdpTask(UdpClient client)
        {
            StringBuilder sb = new();

            while (client.Client.IsBound)
            {
                var res = await client.ReceiveAsync();

                if (res.RemoteEndPoint.Address.ToString() != "192.168.1.110" && res.RemoteEndPoint.Address.ToString() != "192.168.1.101")
                {
                    continue;
                }

                //m_Logger.LogTrace("{data}", BitConverter.ToString(res.Buffer));
                //m_Logger.LogTrace("{data}", Encoding.UTF8.GetString(res.Buffer));

                Packet packet = default;

                sb.Clear();

                try
                {
                    packet = PacketReader.ReadPacket(res.Buffer);
                    sb.AppendLine(packet.ToString());

                    sb.AppendLine("Questions:");
                    foreach (var item in packet.Questions)
                    {
                        sb.AppendLine(item.ToString());
                    }

                    sb.AppendLine("Answers:");
                    foreach (var item in packet.Answers)
                    {
                        sb.AppendLine(item.ToString());
                    }

                    sb.AppendLine("Authorities:");
                    foreach (var item in packet.Authorities)
                    {
                        sb.AppendLine(item.ToString());
                    }

                    sb.AppendLine("Additionals:");
                    foreach (var item in packet.Additionals)
                    {
                        sb.AppendLine(item.ToString());
                    }
                }
                catch (Exception ex)
                {
                    sb.Append(ex.GetType().Name);
                    sb.Append(": ");
                    sb.AppendLine(ex.Message);
                }

                m_Logger.LogInformation("UDP: {remote} {len} bytes\n{packet}", res.RemoteEndPoint, res.Buffer.Length, sb.ToString());

                // todo add periodic annoucements, not just replying
                if (packet != default)
                {
                    ProcessUdpPacket(packet, client);
                }
            }

            client.Dispose();
        }

        private void ProcessUdpPacket(Packet packet, UdpClient client)
        {
            foreach (var question in packet.Questions)
            {
                if (question.Type == PacketRecordData_PTR.Type)
                {
                    if (question.Name == Const.MdnsHapDomainName)
                    {
                        ushort flags = 0;
                        flags = Utils.SetBits(flags, Const.FlagsQueryOrResponsePosition, 1, 1);
                        flags = Utils.SetBits(flags, Const.FlagsAuthoritativeAnswerPosition, 1, 1);

                        var mac = MacAddress.Replace(":", "");
                        var identifier = Name + "@" + mac + "." + Const.MdnsHapDomainName;
                        var host = mac + "." + Const.MdnsLocal;

                        var responsePacket = new Packet()
                        {
                            Header = new PacketHeader()
                            {
                                Flags = flags
                            },
                            Answers = [
                                new PacketRecord()
                                {
                                    Name = Const.MdnsHapDomainName,
                                    Type = PacketRecordData_PTR.Type,
                                    Class = 1,
                                    Ttl = Const.LongTtl,
                                    Data = new PacketRecordData_PTR()
                                    {
                                        Name = identifier
                                    }
                                },
                                new PacketRecord()
                                {
                                    Name = identifier,
                                    Type = PacketRecordData_SRV.Type,
                                    Class = 0x8001,
                                    Ttl = Const.ShortTtl,
                                    Data = new PacketRecordData_SRV()
                                    {
                                        Priority = 0,
                                        Weight = 0,
                                        Port = Port,
                                        Target = host,
                                    }
                                },
                                new PacketRecord()
                                {
                                    Name = identifier,
                                    Type = PacketRecordData_TXT.Type,
                                    Class = 0x8001,
                                    Ttl = Const.LongTtl,
                                    Data = new PacketRecordData_TXT()
                                    {
                                        KeyValuePairs = new Dictionary<string, string>()
                                        {
                                            { "md", Name },
                                            { "id", MacAddress },
                                            { "ci", Category.GetHashCode().ToString() },
                                            { "sh", GenerateSetupHash() },

                                            { "s#", Paired ? "2" : "1" }, // todo state 1=unpaired 2=paired
                                            { "sf", Paired ? "0" : "1" }, // todo status 0=hidden 1=discoverable
                                
                                            { "c#", "1" }, // todo config num, increment when accessory changes
                                
                                            { "pv", "1.1" },
                                            { "ff", "0" },
                                        }
                                    }
                                },
                                new PacketRecord()
                                {
                                    Name = host,
                                    Type = PacketRecordData_A.Type,
                                    Class = 0x8001,
                                    Ttl = Const.ShortTtl,
                                    Data = new PacketRecordData_A()
                                    {
                                        IpAddress = IpAddress
                                    }
                                },
                            ],
                        };


                        var result = PacketWriter.WritePacket(responsePacket);

                        var broadcastEndpoint = new IPEndPoint(IPAddress.Parse(Const.MdnsMulticastAddress), Const.MdnsPort);
                        var length = client.Send(result, result.Length, broadcastEndpoint);
                        m_Logger.LogInformation("UDP: broadcasted packet {length} bytes", length);
                    }
                }
            }
        }

        private string GenerateSetupHash()
        {
            var plain = SetupId + MacAddress;
            var bytes = Encoding.UTF8.GetBytes(plain);
            var hashed = SHA512.HashData(bytes);
            return Convert.ToBase64String(hashed.Take(4).ToArray());
        }

        private void AddAccessoryInformationService()
        {
            var service = new Service(ServiceType.AccessoryInformation);
            service.GetCharacteristic(CharacteristicType.Name)!.SetValue(Name);
            service.GetCharacteristic(CharacteristicType.SerialNumber)!.SetValue("SN-" + Name);

            AddService(service);
        }

        private void AddService(Service service)
        {
            throw new NotImplementedException();
        }

        private void PrintSetupMessage()
        {
            var uri = Utils.GenerateXhmUri(Category, PinCode, SetupId);

            m_Logger.LogTrace("Setup payload: {payload}", uri);
            m_Logger.LogInformation("Setup pincode: {pincode}", PinCode);

            var qrGenerator = new QRCodeGenerator();
            var qrCodeData = qrGenerator.CreateQrCode(uri, QRCodeGenerator.ECCLevel.Q);
            var qrCode = new AsciiQRCode(qrCodeData);
            var qrCodeAsAsciiArt = qrCode.GetGraphic(1);
            m_Logger.LogInformation("{qrcode}", qrCodeAsAsciiArt);

        }

        public void Dispose()
        {
            // todo
        }

    }
}
