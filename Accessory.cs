using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using HomeKit.Hap;
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

        // todo not static
        public static byte[] AccessoryLtSk = null!;
        public static byte[] AccessoryLtPk = null!;

        public static Accessory Temporary_Instance = null!;

        public int Aid { get; }
        public List<Service> Services { get; }

        private readonly string m_Name;
        private readonly string m_PinCode;
        private readonly Category m_Category;
        private readonly string m_SetupId;
        private readonly string m_MacAddress;
        private readonly bool m_Paired;
        private readonly ushort m_Port;
        private readonly IPAddress m_IpAddress;

        private MdnsClient m_MdnsClient = null!;

        /// <param name="pinCode">Format: 000-00-000</param>
        public Accessory(string name, string pinCode, Category category, ILoggerFactory? loggerFactory)
        {
            Aid = 1;
            Services = new();

            m_Name = name;
            m_PinCode = pinCode;
            m_Category = category;
            m_SetupId = Utils.GenerateSetupId();
            m_MacAddress = Utils.GenerateMacAddress();
            //m_MacAddress = "07:44:EF:7F:4D:4F";
            m_Paired = false;
            m_Port = 23232;
            m_IpAddress = IPAddress.Parse("192.168.1.101");

            loggerFactory ??= new NullLoggerFactory();
            m_LoggerFactory = loggerFactory;
            m_Logger = loggerFactory.CreateLogger($"Accessory<{name}>");

            Temporary_Instance = this;

            AddAccessoryInformationService();
        }

        public async Task Publish()
        {
            if (!m_Paired)
            {
                PrintSetupMessage();
            }

            m_Logger.LogTrace("MAC address: {mac}", m_MacAddress);
            m_Logger.LogTrace("Port: {port}", m_Port);
            m_Logger.LogTrace("Address: {address}", m_IpAddress);

            var listener = new TcpListener(IPAddress.Any, m_Port);
            var lobbyTask = TcpLobbyTask(listener);
            // todo store lobby task

            var nis = Utils.GetMulticastNetworkInterfaces();
            var ni = nis.FirstOrDefault(x => x.GetIPProperties().UnicastAddresses.Any(y => y.Address.Equals(m_IpAddress)));
            if (ni is null)
            {
                m_Logger.LogError("Failed to find network interface with specified address");
                return;
            }

            m_Logger.LogTrace("Interface: {interface}", ni.Name);

            m_MdnsClient = new MdnsClient(ni, m_LoggerFactory.CreateLogger<MdnsClient>());
            m_MdnsClient.OnPacketReceived += MdnsClient_OnPacketReceived;
            await m_MdnsClient.StartAsync(CancellationToken.None);

            m_MdnsClient.BroadcastPeriodically(CreateBroadcastPacket());
        }

        private async Task TcpLobbyTask(TcpListener listener)
        {
            listener.Start();

            while (true)
            {
                var client = await listener.AcceptTcpClientAsync();

                m_Logger.LogInformation("Accepted new TCP client {remote}", client.Client.RemoteEndPoint);

                var hapClient = new HapClient(client, m_PinCode, m_MacAddress, m_LoggerFactory.CreateLogger<HapClient>());
                await hapClient.StartAsync(CancellationToken.None);
            }
        }

        private void MdnsClient_OnPacketReceived(Packet packet)
        {
            foreach (var question in packet.Questions)
            {
                if (question.Type != PacketRecordData_PTR.Type)
                {
                    continue;
                }

                if (question.Name != Const.MdnsHapDomainName)
                {
                    continue;
                }

                m_MdnsClient.Broadcast(CreateBroadcastPacket());
            }
        }

        private Packet CreateBroadcastPacket()
        {
            ushort flags = 0;
            flags = Utils.SetBits(flags, Const.FlagsQueryOrResponsePosition, 1, 1);
            flags = Utils.SetBits(flags, Const.FlagsAuthoritativeAnswerPosition, 1, 1);

            var mac = m_MacAddress.Replace(":", "");
            var identifier = m_Name + "@" + mac + "." + Const.MdnsHapDomainName;
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
                            Ttl = PacketRecord.LongTtl,
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
                            Ttl = PacketRecord.ShortTtl,
                            Data = new PacketRecordData_SRV()
                            {
                                Priority = 0,
                                Weight = 0,
                                Port = m_Port,
                                Target = host,
                            }
                        },
                        new PacketRecord()
                        {
                            Name = identifier,
                            Type = PacketRecordData_TXT.Type,
                            Class = 0x8001,
                            Ttl = PacketRecord.LongTtl,
                            Data = new PacketRecordData_TXT()
                            {
                                KeyValuePairs = new Dictionary<string, string>()
                                {
                                    /// 6.4
                                    { "md", m_Name },
                                    { "id", m_MacAddress },
                                    { "ci", m_Category.GetHashCode().ToString() },
                                    { "sh", GenerateSetupHash() },

                                    { "s#", m_Paired ? "2" : "1" }, // todo state 1=unpaired 2=paired
                                    { "sf", m_Paired ? "0" : "1" }, // todo status 0=hidden 1=discoverable
                                
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
                            Ttl = PacketRecord.ShortTtl,
                            Data = new PacketRecordData_A()
                            {
                                IpAddress = m_IpAddress
                            }
                        },
                    ],
            };

            return responsePacket;
        }

        private string GenerateSetupHash()
        {
            var plain = m_SetupId + m_MacAddress;
            var bytes = Encoding.UTF8.GetBytes(plain);
            var hashed = SHA512.HashData(bytes);
            return Convert.ToBase64String(hashed.Take(4).ToArray());
        }

        private void AddAccessoryInformationService()
        {
            var service = new Service(ServiceType.AccessoryInformation);
            service.GetCharacteristic(CharacteristicType.Name)!.Value = m_Name;
            service.GetCharacteristic(CharacteristicType.SerialNumber)!.Value = "SN-" + m_Name;

            Services.Add(service);
        }

        public Service AddService(ServiceType type)
        {
            var service = new Service(type);
            Services.Add(service);
            return service;
        }

        private void PrintSetupMessage()
        {
            var uri = Utils.GenerateXhmUri(m_Category, m_PinCode, m_SetupId);

            m_Logger.LogTrace("Setup payload: {payload}", uri);
            m_Logger.LogInformation("Setup pincode: {pincode}", m_PinCode);

            var qrGenerator = new QRCodeGenerator();
            var qrCodeData = qrGenerator.CreateQrCode(uri, QRCodeGenerator.ECCLevel.Q);
            var qrCode = new AsciiQRCode(qrCodeData);
            var qrCodeAsAsciiArt = qrCode.GetGraphic(1);
            m_Logger.LogCritical("{qrcode}", qrCodeAsAsciiArt);

        }

        public void Dispose()
        {
            // todo
        }

    }
}
