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

            var listener = new TcpListener(IPAddress.Any, Port);
            Task.Run(() => ServerReceptionTask(listener));

            foreach (var ni in Utils.GetMulticastNetworkInterfaces())
            {
                var adapterIndex = ni.GetIPProperties().GetIPv4Properties().Index;
                var client = new UdpClient();
                var socket = client.Client;

                socket.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.MulticastInterface, IPAddress.HostToNetworkOrder(adapterIndex));
                client.ExclusiveAddressUse = false;
                socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
                var localEp = new IPEndPoint(IPAddress.Any, 5353);
                socket.Bind(localEp);
                var multicastAddress = IPAddress.Parse("224.0.0.251");
                var multOpt = new MulticastOption(multicastAddress, adapterIndex);
                socket.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.AddMembership, multOpt);

                Task.Run(() => ClientTask(client));
            }
        }

        private async Task ServerReceptionTask(TcpListener listener)
        {
            listener.Start();

            while (true)
            {
                var client = await listener.AcceptTcpClientAsync();

                m_Logger.LogInformation("Accepted new TCP client {remote}", client.Client.RemoteEndPoint);

                _ = Task.Run(() => ServerTask(client));
            }
        }

        private async Task ServerTask(TcpClient client)
        {
            var buffer = new Memory<byte>();
            var stream = client.GetStream();

            while (client.Connected)
            {
                var length = await stream.ReadAsync(buffer);
                m_Logger.LogInformation("Received TCP data {length}", length);
            }
        }

        private async Task ClientTask(UdpClient client)
        {
            StringBuilder sb = new();

            while (client.Client.IsBound)
            {
                var res = await client.ReceiveAsync();

                if (res.RemoteEndPoint.Address.ToString() != "192.168.1.110")
                {
                    continue;
                }

                //m_Logger.LogTrace("{data}", BitConverter.ToString(res.Buffer));

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
                }
                catch (Exception ex)
                {
                    sb.Append(ex.GetType().Name);
                    sb.Append(": ");
                    sb.AppendLine(ex.Message);
                }

                m_Logger.LogInformation("UDP: {remote} {len} bytes\n{packet}", res.RemoteEndPoint, res.Buffer.Length, sb.ToString());

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
                        // todo broadcast announce packet (MakeAnnouncePacket)

                        ushort flags = 0;
                        flags = Utils.SetBits(flags, Const.FlagsQueryOrResponsePosition, 1, 1);
                        flags = Utils.SetBits(flags, Const.FlagsAuthoritativeAnswerPosition, 1, 1);

                        var name = Name + "_" + MacAddress.Replace(":", "");

                        var longName = name + "." + Const.MdnsHapDomainName;
                        var shortName = name + "." + Const.MdnsLocal;

                        // todo try to add 2 additionals

                        var responsePacket = new Packet()
                        {
                            Header = new PacketHeader()
                            {
                                Flags = flags
                            },
                            Answers = [
                                new PacketRecord()
                                {
                                    Name = longName,
                                    Type = PacketRecordData_SRV.Type,
                                    Class = 0x8001,
                                    Ttl = Const.ShortTtl,
                                    Data = new PacketRecordData_SRV()
                                    {
                                        Priority = 0,
                                        Weight = 0,
                                        Port = Port,
                                        Target = shortName,
                                    }
                                },
                                new PacketRecord()
                                {
                                    Name = longName,
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
                                    Name = "_services._dns-sd._udp.local.",
                                    Type = PacketRecordData_PTR.Type,
                                    Class = 1,
                                    Ttl = Const.LongTtl,
                                    Data = new PacketRecordData_PTR()
                                    {
                                        Name = Const.MdnsHapDomainName
                                    }
                                },
                                new PacketRecord()
                                {
                                    Name = Const.MdnsHapDomainName,
                                    Type = PacketRecordData_PTR.Type,
                                    Class = 1,
                                    Ttl = Const.LongTtl,
                                    Data = new PacketRecordData_PTR()
                                    {
                                        Name = longName
                                    }
                                },
                                new PacketRecord()
                                {
                                    Name = shortName,
                                    Type = PacketRecordData_A.Type,
                                    Class = 0x8001,
                                    Ttl = Const.ShortTtl,
                                    Data = new PacketRecordData_A()
                                    {
                                        IpAddress = "192.168.1.101" // todo
                                    }
                                },
                                new PacketRecord()
                                {
                                    Name = "101.1.168.192.in-addr.arpa.", // todo
                                    Type = PacketRecordData_PTR.Type,
                                    Class = 0x8001,
                                    Ttl = Const.ShortTtl,
                                    Data = new PacketRecordData_PTR()
                                    {
                                        Name = shortName
                                    }
                                },
                            ],
                        };


                        var result = PacketWriter.WritePacket(responsePacket);

                        var broadcastEndpoint = new IPEndPoint(IPAddress.Parse("224.0.0.251"), 5353);
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

        // todo validate if works correctly
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
            //throw new NotImplementedException();
        }

    }
}
