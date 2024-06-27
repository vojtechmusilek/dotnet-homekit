﻿using System;
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

        public string Name { get; }
        public string PinCode { get; }
        public Category Category { get; }
        public string SetupId { get; }
        public string MacAddress { get; }
        public bool Paired { get; }
        public ushort Port { get; }
        public IPAddress IpAddress { get; }

        private MdnsClient m_MdnsClient = null!;

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

            m_MdnsClient = new MdnsClient(ni, m_LoggerFactory.CreateLogger<MdnsClient>());
            m_MdnsClient.OnPacketReceived += MdnsClient_OnPacketReceived;
            m_MdnsClient.StartAsync(CancellationToken.None);
        }

        private void MdnsClient_OnPacketReceived(Packet packet)
        {
            // todo remove temporary filter
            if (packet.Endpoint.Address.ToString() != "192.168.1.110" && packet.Endpoint.Address.ToString() != "192.168.1.101")
            {
                return;
            }

            m_Logger.LogInformation("Received packet {packet}", packet);

            //if (packet.Endpoint.Address.Equals(IpAddress))
            //{
            //    return;
            //}

            var response = RespondToPacket(packet);
            if (response.HasValue)
            {
                m_MdnsClient.Broadcast(response.Value);
            }
        }

        private async Task TcpLobbyTask(TcpListener listener)
        {
            listener.Start();

            while (true)
            {
                m_Logger.LogInformation("Waiting for TCP client...");

                var client = await listener.AcceptTcpClientAsync();

                m_Logger.LogInformation("Accepted new TCP client {remote}", client.Client.RemoteEndPoint);

                var hapClient = new HapClient(client, m_LoggerFactory.CreateLogger<HapClient>());
                await hapClient.StartAsync(CancellationToken.None);
            }
        }

        private Packet? RespondToPacket(Packet packet)
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

                return responsePacket;
            }

            return null;
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
