using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
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
        private static readonly QRCodeGenerator m_QRCodeGenerator = new();

        private readonly ILoggerFactory m_LoggerFactory;
        private readonly ILogger m_Logger;

        private readonly HapSessionCollection m_HapSessions = new();
        private readonly HashSet<Accessory> m_Accessories = new();
        private readonly HashSet<MdnsClient> m_MdnsClients = new();

        private readonly IPAddress m_IpAddress;
        private readonly ushort m_Port;
        private readonly string m_PinCode;
        private readonly string m_SetupId;
        private readonly string m_SetupHash;
        private readonly string m_MacAddress;
        private readonly uint m_BroadcastIntervalSeconds;

        private readonly ServerState m_State;
        private readonly string m_StateFilePath;

        private readonly string m_Name;
        private readonly Category m_Category;
        private readonly ushort m_MaxClients;

        private Task? m_SessionCreatorTask;
        private CancellationTokenSource m_StoppingToken = null!;

        public HashSet<Accessory> Accessories => m_Accessories;

        public AccessoryServer(AccessoryServerOptions options)
        {
            m_IpAddress = Utils.GetServerIpAddress(options.IpAddress ?? "");
            m_Port = options.Port ?? Utils.GetAvailablePort(m_IpAddress);

            m_PinCode = options.PinCode ?? Utils.GeneratePinCode();
            m_MacAddress = options.MacAddress ?? Utils.GenerateMacAddress();
            m_SetupId = Utils.GenerateSetupId();
            m_SetupHash = Utils.GenerateSetupHash(m_SetupId, m_MacAddress);
            m_BroadcastIntervalSeconds = options.BroadcastIntervalSeconds ?? (options.Port.HasValue ? PacketRecord.ShortTtl : 3);

            m_LoggerFactory = options.LoggerFactory ?? NullLoggerFactory.Instance;
            m_Logger = m_LoggerFactory.CreateLogger<AccessoryServer>();

            var directoryPath = options.StateDirectory ?? AppDomain.CurrentDomain.BaseDirectory;
            m_StateFilePath = Path.Join(directoryPath, $"server_state_{m_MacAddress.Replace(':', '_')}.json");

            m_MaxClients = options.MaxClients ?? ushort.MaxValue;
            m_Name = options.Name ?? "DefaultAccessory";
            m_Category = options.Category ?? Category.Other;

            m_Logger.LogTrace("Loading state at {Path}", m_StateFilePath);

            m_State = ServerState.Load(m_StateFilePath, out var newState);
            if (newState)
            {
                m_Logger.LogInformation("State was not found, created new state");
            }

            m_Logger.LogTrace("State has {Count} clients", m_State.PairedClients.Count);
        }

        public PairedClient? GetPairedClient(Guid id)
        {
            return m_State.PairedClients.Find(x => x.Id == id);
        }

        public ReadOnlySpan<PairedClient> GetPairedClients()
        {
            return CollectionsMarshal.AsSpan(m_State.PairedClients);
        }

        /// <returns>true if added, false if not added because of max limit</returns>
        public bool TryAddPairedClient(PairedClient pairedClient)
        {
            var existing = m_State.PairedClients.Find(x => x.Id == pairedClient.Id);

            var countAfterAdd = m_State.PairedClients.Count + (existing == null ? 1 : 0);
            if (countAfterAdd > m_MaxClients)
            {
                return false;
            }

            if (existing is not null)
            {
                m_State.PairedClients.Remove(existing);
            }

            m_State.PairedClients.Add(pairedClient);
            m_State.Save(m_StateFilePath);
            return true;
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

        public bool HasNotBeenPaired()
        {
            return m_State.PairedClients.Count == 0;
            // the following doesnt really work when removing accesory and adding it right back:
            // check if we have space for another pairing and if we do then act like nothing has been paired
            // return !HasReachedMaximumPairings();
        }

        public bool HasReachedMaximumPairings()
        {
            return m_State.PairedClients.Count >= m_MaxClients;
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
            CheckVersion();
            PrintSetupMessage();

            await SetupHap(m_StoppingToken.Token);
            await SetupMdns(m_StoppingToken.Token);
        }

        public async Task StopAsync()
        {
            await m_StoppingToken.CancelAsync();

            await m_HapSessions.StopAsync();

            if (m_SessionCreatorTask is not null)
            {
                await m_SessionCreatorTask;
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
                        characteristic.Aid = accessory.Aid;
                    }
                }
            }
        }

        private void CheckVersion()
        {
            // for now just add Aid and Iid, that should be unique enough
            uint currentHash = 0;
            foreach (var accessory in Accessories)
            {
                currentHash += (uint)accessory.Aid;
                foreach (var service in accessory.Services)
                {
                    currentHash += (uint)service.Iid;
                    foreach (var characteristic in service.Characteristics)
                    {
                        currentHash += (uint)characteristic.Iid;
                    }
                }
            }

            if (currentHash == m_State.AccessoryHash)
            {
                m_Logger.LogInformation("Accessory hash is same, using version {version}", m_State.AccessoryVersion);
            }
            else
            {
                m_State.AccessoryVersion += 1;
                m_State.AccessoryHash = currentHash;
                m_State.Save(m_StateFilePath);
                m_Logger.LogWarning("Accessory hash has changed, using new version {version}", m_State.AccessoryVersion);
            }
        }

        private Task SetupHap(CancellationToken stoppingToken)
        {
            m_SessionCreatorTask = SessionCreatorTask(stoppingToken);
            return Task.CompletedTask;
        }

        private async Task SetupMdns(CancellationToken stoppingToken)
        {
            m_Logger.LogInformation("Searching for available network interfaces");

            var interfaces = Utils.GetMulticastNetworkInterfaces();
            NetworkInterface? prefferedInterface = null;

            foreach (var nix in interfaces)
            {
                foreach (var item in nix.GetIPProperties().UnicastAddresses)
                {
                    if (item.Address.AddressFamily == AddressFamily.InterNetwork)
                    {
                        if (item.Address.Equals(m_IpAddress))
                        {
                            prefferedInterface = nix;
                        }

                        m_Logger.LogInformation("Available interface {Name}, {IpAddress}", nix.Name, item.Address);
                        break;
                    }
                }
            }

            if (prefferedInterface is not null)
            {
                m_Logger.LogInformation("Using preffered interface {Name}", prefferedInterface.Name);

                var mdnsClient = CreateMdnsClient(prefferedInterface);
                await mdnsClient.StartAsync(stoppingToken);

                return;
            }

            m_Logger.LogInformation("Using all {Count} interfaces", interfaces.Length);

            foreach (var networkInterface in interfaces)
            {
                var mdnsClient = CreateMdnsClient(networkInterface);
                await mdnsClient.StartAsync(stoppingToken);
            }
        }

        private MdnsClient CreateMdnsClient(NetworkInterface networkInterface)
        {
            var interval = TimeSpan.FromSeconds(m_BroadcastIntervalSeconds);
            var mdnsClient = new MdnsClient(networkInterface, interval, m_LoggerFactory.CreateLogger<MdnsClient>());
            mdnsClient.OnPacketReceived += MdnsClient_OnPacketReceived;
            mdnsClient.BroadcastPeriodically(CreateBroadcastPacket);
            m_MdnsClients.Add(mdnsClient);
            return mdnsClient;
        }

        private async Task SessionCreatorTask(CancellationToken stoppingToken)
        {
            // https://stackoverflow.com/a/67442253/7838578
            var listener = new TcpListener(IPAddress.Any, m_Port);
            listener.Start();

            while (!stoppingToken.IsCancellationRequested)
            {
                var client = await listener.AcceptTcpClientAsync(stoppingToken);
                if (client.Client.RemoteEndPoint is not IPEndPoint ipEndPoint)
                {
                    continue;
                }

                var session = new HapSession(this, client, m_LoggerFactory.CreateLogger<HapSession>());
                m_Logger.LogInformation("Created new session for {remote}", session.Remote);

                await m_HapSessions.AddAndStart(session, ipEndPoint, stoppingToken);
            }
        }

        private void MdnsClient_OnPacketReceived(MdnsClient sender, Packet packet)
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

                sender?.Broadcast(CreateBroadcastPacket());
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
                                { "c#", m_State.AccessoryVersion.ToString() }, // Current configuration number
                                { "ff", HasReachedMaximumPairings() ? "4" : "0" }, // Pairing feature flags
                                { "id", m_MacAddress }, // Device ID
                                { "md", m_Name }, // Model name
                                { "pv", "1.1" }, // Protocol version string
                                { "s#", "1" }, // Current state number (must be 1)
                                { "sf", HasNotBeenPaired() ? "1" : "0" }, // Status flags
                                { "ci", m_Category.GetHashCode().ToString() }, // Category identifier
                                { "sh", m_SetupHash }, // Setup hash
                            }
                        }
                    },
                },
            };

            return responsePacket;
        }

        private void PrintSetupMessage()
        {
            var uri = Utils.GenerateXhmUri(m_Category, m_PinCode, m_SetupId);
            var data = m_QRCodeGenerator.CreateQrCode(uri, QRCodeGenerator.ECCLevel.M);
            var qrCode = new AsciiQRCode(data).GetGraphic(1);

            if (m_LoggerFactory is NullLoggerFactory)
            {
                Console.WriteLine($"Accessory: {m_Name} ({m_Category})");
                Console.WriteLine($"Host: {m_IpAddress}:{m_Port}");
                Console.WriteLine($"MacAddress: {m_MacAddress}");
                Console.WriteLine($"PinCode: {m_PinCode}");
                Console.WriteLine(qrCode);
            }
            else
            {
                m_Logger.LogInformation("Accessory: {Name} ({Category})", m_Name, m_Category);
                m_Logger.LogInformation("Host: {IpAddress}:{Port}", m_IpAddress, m_Port);
                m_Logger.LogInformation("MacAddress: {MacAddress}", m_MacAddress);
                m_Logger.LogInformation("PinCode: {PinCode}", m_PinCode);
                m_Logger.LogInformation("{QrCode}", qrCode);
            }
        }
    }
}
