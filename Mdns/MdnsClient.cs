using System;
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

        private Task m_ReceiverTask = null!;
        private CancellationTokenSource m_StoppingToken = null!;

        public event Action<Packet>? OnPacketReceived;

        public MdnsClient(NetworkInterface networkInterface, ILogger<MdnsClient> logger)
        {
            m_Logger = logger;

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
            return Task.CompletedTask;
        }

        public async Task StopAsync()
        {
            await m_StoppingToken.CancelAsync();
            await m_ReceiverTask;
        }

        private async Task ReceiverTask(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested && m_UdpClient.Client.IsBound)
            {
                var res = await m_UdpClient.ReceiveAsync(stoppingToken);

                try
                {
                    var packet = PacketReader.ReadPacket(res.Buffer);
                    packet.Endpoint = res.RemoteEndPoint;

                    m_Logger.LogTrace("Received {length} bytes from {remote}", res.Buffer.Length, res.RemoteEndPoint);
                    OnPacketReceived?.Invoke(packet);
                }
                catch (Exception ex)
                {
                    m_Logger.LogError("Failed to read packet ({error})", ex.Message);
                    continue;
                }
            }
        }

        public void Broadcast(Packet packet)
        {
            var buffer = PacketWriter.WritePacket(packet);
            var length = m_UdpClient.Send(buffer, buffer.Length, m_BroadcastEndpoint);

            m_Logger.LogInformation("Broadcasted {length} bytes", length);
        }
    }
}
