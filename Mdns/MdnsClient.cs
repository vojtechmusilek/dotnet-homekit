﻿using System;
using System.Collections.Generic;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace HomeKit.Mdns
{
    internal class MdnsClient
    {
        public const string MdnsMulticastAddress = "224.0.0.251";
        public const ushort MdnsPort = 5353;

        private readonly ILogger m_Logger;
        private readonly UdpClient m_UdpClient;
        private readonly IPEndPoint m_BroadcastEndpoint;

        private CancellationTokenSource m_StoppingToken = null!;
        private Task m_ReceiverTask = null!;
        private Task m_BroadcasterTask = null!;

        private readonly PeriodicTimer m_BroadcasterTimer;
        private readonly List<Func<Packet>> m_BroadcasterPacketGetters = new();

        public event Action<MdnsClient, Packet>? OnPacketReceived;

        public MdnsClient(NetworkInterface networkInterface, TimeSpan broadcastInterval, ILogger<MdnsClient> logger)
        {
            m_Logger = logger;
            m_BroadcasterTimer = new(broadcastInterval);
            var adapterIndex = networkInterface.GetIPProperties().GetIPv4Properties().Index;
            var multicastInterface = IPAddress.HostToNetworkOrder(adapterIndex);
            var membership = new MulticastOption(IPAddress.Parse(MdnsMulticastAddress), adapterIndex);

            m_UdpClient = new UdpClient();
            m_UdpClient.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
            m_UdpClient.Client.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.MulticastInterface, multicastInterface);
            m_UdpClient.Client.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.AddMembership, membership);
            m_UdpClient.Client.Bind(new IPEndPoint(IPAddress.Any, MdnsPort));

            m_BroadcastEndpoint = new IPEndPoint(IPAddress.Parse(MdnsMulticastAddress), MdnsPort);
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            m_StoppingToken = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            m_ReceiverTask = ReceiverTask(m_StoppingToken.Token);
            m_BroadcasterTask = BroadcasterTask(m_StoppingToken.Token);
            return Task.CompletedTask;
        }

        public async Task StopAsync()
        {
            await m_StoppingToken.CancelAsync();
            await m_ReceiverTask;
            await m_BroadcasterTask;
        }

        private async Task ReceiverTask(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested && m_UdpClient.Client.IsBound)
            {
                var res = await m_UdpClient.ReceiveAsync(stoppingToken);

                try
                {
                    var packet = PacketReader.ReadPacket(res.Buffer);

                    lock (m_Logger)
                    {
                        m_Logger.LogTrace("Parsed {length} bytes packet from {remote} {header}", res.Buffer.Length, res.RemoteEndPoint, packet.Header);

                        foreach (var question in packet.Questions)
                        {
                            m_Logger.LogTrace("Q: {question}", question.ToString());
                        }

                        foreach (var answer in packet.Answers)
                        {
                            m_Logger.LogTrace("A: {answer}", answer.ToString());
                        }
                    }

                    OnPacketReceived?.Invoke(this, packet);
                }
                catch (Exception ex)
                {
                    m_Logger.LogError("Failed to read packet ({error})", ex.Message);
                    continue;
                }
            }
        }

        private async Task BroadcasterTask(CancellationToken stoppingToken)
        {
            while (await m_BroadcasterTimer.WaitForNextTickAsync(stoppingToken))
            {
                for (int i = 0; i < m_BroadcasterPacketGetters.Count; i++)
                {
                    Broadcast(m_BroadcasterPacketGetters[i].Invoke());
                }
            }
        }

        public void Broadcast(Packet packet)
        {
            var buffer = PacketWriter.WritePacket(packet);
            var length = m_UdpClient.Send(buffer, buffer.Length, m_BroadcastEndpoint);

            m_Logger.LogInformation("Broadcasted {length} bytes", length);
        }

        public void BroadcastPeriodically(Func<Packet> packetGetter)
        {
            m_BroadcasterPacketGetters.Add(packetGetter);
            Broadcast(packetGetter.Invoke());
        }
    }
}
