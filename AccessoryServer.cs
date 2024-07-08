using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
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
        private readonly ILoggerFactory m_LoggerFactory;
        private readonly ILogger m_Logger;

        private MdnsClient? m_MdnsClient = null!;
        private Task? m_ClientReceiverTask;
        private CancellationTokenSource m_StoppingToken = null!;

        private readonly HashSet<HapClient> m_Clients = new();
        private readonly HashSet<Accessory> m_Accessories = new();

        private readonly IPAddress m_IpAddress;
        private readonly ushort m_Port;
        private readonly string m_PinCode;
        private readonly string m_SetupId;
        private readonly string m_MacAddress;

        private readonly ServerState m_State;
        private readonly string m_StateFilePath;

        public HashSet<Accessory> Accessories => m_Accessories;

        public AccessoryServer(AccessoryServerOptions options)
        {
            m_IpAddress = string.IsNullOrWhiteSpace(options.IpAddress) ? IPAddress.Any : IPAddress.Parse(options.IpAddress);
            m_Port = options.Port ?? 23232;

            m_PinCode = options.PinCode ?? Utils.GeneratePinCode();
            m_MacAddress = options.MacAddress ?? Utils.GenerateMacAddress();
            m_SetupId = Utils.GenerateSetupId();

            m_LoggerFactory = options.LoggerFactory ?? NullLoggerFactory.Instance;
            m_Logger = m_LoggerFactory.CreateLogger<AccessoryServer>();

            var directoryPath = Path.GetDirectoryName(options.StateDirectory ?? AppDomain.CurrentDomain.BaseDirectory);
            m_StateFilePath = Path.Join(directoryPath, $"server_state_{m_MacAddress.Replace(':', '_')}.json");

            m_State = ServerState.Load(m_StateFilePath);
        }

        public PairedClient? GetPairedClient(Guid id)
        {
            return m_State.PairedClients.Find(x => x.Id == id);
        }

        public ReadOnlySpan<PairedClient> GetPairedClients()
        {
            return CollectionsMarshal.AsSpan(m_State.PairedClients);
        }

        public void AddPairedClient(PairedClient pairedClient)
        {
            m_State.PairedClients.Add(pairedClient);
            m_State.Save(m_StateFilePath);
        }

        public void RemovePairedClient(Guid id)
        {
            var client = GetPairedClient(id);
            if (client is not null)
            {
                m_State.PairedClients.Remove(client);
            }

            m_State.Save(m_StateFilePath);
        }

        public bool IsPaired()
        {
            return m_State.PairedClients.Count > 0;
        }

        public ReadOnlySpan<byte> GetLtSk()
        {
            return m_State.ServerLtSk;
        }

        public ReadOnlySpan<byte> GetLtPk()
        {
            return m_State.ServerLtPk;
        }

        public string GetPinCode()
        {
            return m_PinCode;
        }

        public string GetMacAddress()
        {
            return m_MacAddress;
        }

        public Characteristic? GetCharacteristic(int aid, int iid)
        {
            return Accessories
                .FirstOrDefault(acc => acc.Aid == aid)?.Services
                .SelectMany(ser => ser.Characteristics)
                .FirstOrDefault(cha => cha.Iid == iid);
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            if (m_StoppingToken is not null)
            {
                throw new InvalidOperationException("Server already started");
            }

            m_StoppingToken = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

            AssignIds();
            PrintSetupMessage();

            await SetupHap(m_StoppingToken.Token);
            await SetupMdns(m_StoppingToken.Token);
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            await m_StoppingToken.CancelAsync();

            await Task.WhenAll(m_Clients.Select(x => x.StopAsync()));

            if (m_ClientReceiverTask is not null)
            {
                await m_ClientReceiverTask;
            }
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

        private Task SetupHap(CancellationToken stoppingToken)
        {
            m_ClientReceiverTask = ClientReceiverTask(stoppingToken);
            return Task.CompletedTask;
        }

        private async Task SetupMdns(CancellationToken stoppingToken)
        {
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
            m_MdnsClient.BroadcastPeriodically(CreateBroadcastPacket());

            await m_MdnsClient.StartAsync(stoppingToken);
        }

        private async Task ClientReceiverTask(CancellationToken stoppingToken)
        {
            var listener = new TcpListener(IPAddress.Any, m_Port);
            listener.Start();

            while (!stoppingToken.IsCancellationRequested)
            {
                var client = await listener.AcceptTcpClientAsync(stoppingToken);

                m_Logger.LogInformation("Accepted new TCP client {remote}", client.Client.RemoteEndPoint);

                var hapClient = new HapClient(this, client, m_LoggerFactory.CreateLogger<HapClient>());
                m_Clients.Add(hapClient);

                await hapClient.StartAsync(stoppingToken);
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

                m_MdnsClient?.Broadcast(CreateBroadcastPacket());
            }
        }

        private Packet CreateBroadcastPacket()
        {
            ushort flags = 0;
            flags = Utils.SetBits(flags, Const.FlagsQueryOrResponsePosition, 1, 1);
            flags = Utils.SetBits(flags, Const.FlagsAuthoritativeAnswerPosition, 1, 1);

            var firstAccessory = Accessories.First();

            var mac = m_MacAddress.Replace(":", "");
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
                            IpAddress = m_IpAddress
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
                            Port = m_Port,
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
                                { "id", m_MacAddress },
                                { "ci", firstAccessory.Category.GetHashCode().ToString() },
                                { "sh", Utils.GenerateSetupHash(m_SetupId, m_MacAddress) },

                                { "s#", IsPaired() ? "2" : "1" }, // todo state 1=unpaired 2=paired
                                { "sf", IsPaired() ? "0" : "1" }, // todo status 0=hidden 1=discoverable
                        
                                { "c#", "2" }, // todo config num, increment when accessory changes
                                
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

            m_Logger.LogTrace("IpAddress: {IpAddress}", m_IpAddress);
            m_Logger.LogTrace("Port: {Port}", m_Port);
            m_Logger.LogTrace("PinCode: {PinCode}", m_PinCode);
            m_Logger.LogTrace("MacAddress: {MacAddress}", m_MacAddress);

            var qrGenerator = new QRCodeGenerator();
            var uri = Utils.GenerateXhmUri(firstAccessory.Category, m_PinCode, m_SetupId);
            var qrCodeData = qrGenerator.CreateQrCode(uri, QRCodeGenerator.ECCLevel.M);
            var qrCode = new AsciiQRCode(qrCodeData);
            var qrCodeAsAsciiArt = qrCode.GetGraphic(1);

            m_Logger.LogInformation("{QrCode}", qrCodeAsAsciiArt);
        }
    }
}
