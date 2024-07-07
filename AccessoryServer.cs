using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text.Json.Serialization;
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
    public class AccessoryServer
    {
        private MdnsClient m_MdnsClient = null!;

        private Task? m_ClientReceiverTask;

        private readonly HashSet<HapClient> m_Clients = new();

        private readonly ILoggerFactory m_LoggerFactory;
        private readonly ILogger m_Logger;

        public HashSet<Accessory> Accessories { get; }

        [JsonIgnore] public IPAddress IpAddress { get; }
        [JsonIgnore] public ushort Port { get; }
        [JsonIgnore] public string PinCode { get; }
        [JsonIgnore] public string SetupId { get; }
        [JsonIgnore] public string MacAddress { get; }

        [JsonIgnore] public ServerState State { get; }

        public AccessoryServer(string? ipAddress = null, ushort? port = null, string? pinCode = null, string? macAddress = null, string? statePath = null, ILoggerFactory? loggerFactory = null)
        {
            Accessories = new();

            IpAddress = string.IsNullOrWhiteSpace(ipAddress) ? IPAddress.Any : IPAddress.Parse(ipAddress);
            Port = port ?? 23232;

            PinCode = pinCode ?? Utils.GeneratePinCode();
            MacAddress = macAddress ?? Utils.GenerateMacAddress();
            SetupId = Utils.GenerateSetupId();

            m_LoggerFactory = loggerFactory ?? NullLoggerFactory.Instance;
            m_Logger = m_LoggerFactory.CreateLogger<AccessoryServer>();

            State = ServerState.FromPath(statePath ?? AppDomain.CurrentDomain.BaseDirectory, MacAddress);
        }

        public PairedClient? GetPairedClient(Guid id)
        {
            return State.PairedClients.Find(x => x.Id == id);
        }

        public void RemovePairedClient(Guid id)
        {
            var client = GetPairedClient(id);
            if (client is not null)
            {
                State.PairedClients.Remove(client);
            }
        }

        public bool IsPaired()
        {
            return State.PairedClients.Count > 0;
        }

        public Characteristic? GetCharacteristic(int aid, int iid)
        {
            return Accessories
                .FirstOrDefault(acc => acc.Aid == aid)?.Services
                .SelectMany(ser => ser.Characteristics)
                .FirstOrDefault(cha => cha.Iid == iid);
        }

        public async Task Publish(CancellationToken cancellationToken)
        {
            AssignIds();
            PrintSetupMessage();

            await SetupHap(cancellationToken);
            await SetupMdns(cancellationToken);
        }

        private void AssignIds()
        {
            var aid = 1;
            var iid = 1;

            foreach (var accessory in Accessories)
            {
                accessory.Aid = aid++;

                foreach (var service in accessory.Services)
                {
                    service.Iid = iid++;

                    foreach (var characteristic in service.Characteristics)
                    {
                        characteristic.Iid = iid++;
                    }
                }
            }
        }

        private Task SetupHap(CancellationToken cancellationToken)
        {
            m_ClientReceiverTask = ClientReceiverTask(cancellationToken);
            return Task.CompletedTask;
        }

        private async Task SetupMdns(CancellationToken cancellationToken)
        {
            var nis = Utils.GetMulticastNetworkInterfaces();
            var ni = nis.FirstOrDefault(x => x.GetIPProperties().UnicastAddresses.Any(y => y.Address.Equals(IpAddress)));
            if (ni is null)
            {
                m_Logger.LogError("Failed to find network interface with specified address");
                return;
            }

            m_Logger.LogTrace("Interface: {interface}", ni.Name);

            m_MdnsClient = new MdnsClient(ni, m_LoggerFactory.CreateLogger<MdnsClient>());
            m_MdnsClient.OnPacketReceived += MdnsClient_OnPacketReceived;
            m_MdnsClient.BroadcastPeriodically(CreateBroadcastPacket());

            await m_MdnsClient.StartAsync(cancellationToken);
        }

        private async Task ClientReceiverTask(CancellationToken cancellationToken)
        {
            // todo IPAddress.Any?
            var listener = new TcpListener(IPAddress.Any, Port);
            listener.Start();

            while (!cancellationToken.IsCancellationRequested)
            {
                var client = await listener.AcceptTcpClientAsync(cancellationToken);

                m_Logger.LogInformation("Accepted new TCP client {remote}", client.Client.RemoteEndPoint);

                var hapClient = new HapClient(this, client, m_LoggerFactory.CreateLogger<HapClient>());
                m_Clients.Add(hapClient);

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

            var firstAccessory = Accessories.First();

            var mac = MacAddress.Replace(":", "");
            var identifier = firstAccessory.Name + "@" + mac + "." + Const.MdnsHapDomainName;
            var host = mac + "." + Const.MdnsLocal;

            var responsePacket = new Packet()
            {
                Header = new PacketHeader()
                {
                    Flags = flags
                },
                Answers = new PacketRecord[] {
                    new()
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
                    new()
                    {
                        Name = host,
                        Type = PacketRecordData_A.Type,
                        Class = 0x8001,
                        Ttl = PacketRecord.ShortTtl,
                        Data = new PacketRecordData_A()
                        {
                            IpAddress = IpAddress
                        }
                    },
                    new()
                    {
                        Name = identifier,
                        Type = PacketRecordData_SRV.Type,
                        Class = 0x8001,
                        Ttl = PacketRecord.ShortTtl,
                        Data = new PacketRecordData_SRV()
                        {
                            Priority = 0,
                            Weight = 0,
                            Port = Port,
                            Target = host,
                        }
                    },
                    new()
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
                                { "md", firstAccessory.Name },
                                { "id", MacAddress },
                                { "ci", firstAccessory.Category.GetHashCode().ToString() },
                                { "sh", Utils.GenerateSetupHash(SetupId, MacAddress) },

                                { "s#", IsPaired() ? "2" : "1" }, // todo state 1=unpaired 2=paired
                                { "sf", IsPaired() ? "0" : "1" }, // todo status 0=hidden 1=discoverable
                        
                                { "c#", "1" }, // todo config num, increment when accessory changes
                                
                                { "pv", "1.1" },
                                { "ff", "0" },
                            }
                        }
                    },
                },
            };

            return responsePacket;
        }

        private void PrintSetupMessage()
        {
            var firstAccessory = Accessories.First();

            m_Logger.LogTrace("MacAddress: {MacAddress}", MacAddress);
            m_Logger.LogTrace("IpAddress: {IpAddress}", IpAddress);
            m_Logger.LogTrace("Port: {Port}", Port);
            m_Logger.LogTrace("PinCode: {PinCode}", PinCode);

            var qrGenerator = new QRCodeGenerator();
            var uri = Utils.GenerateXhmUri(firstAccessory.Category, PinCode, SetupId);
            var qrCodeData = qrGenerator.CreateQrCode(uri, QRCodeGenerator.ECCLevel.Q); // todo level
            var qrCode = new AsciiQRCode(qrCodeData);
            var qrCodeAsAsciiArt = qrCode.GetGraphic(1);

            m_Logger.LogInformation("{QrCode}", qrCodeAsAsciiArt);
        }
    }
}
