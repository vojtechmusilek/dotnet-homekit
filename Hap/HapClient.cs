using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace HomeKit.Hap
{
    internal class HapClient
    {
        private readonly ILogger m_Logger;
        private readonly Socket m_Socket;
        private readonly NetworkStream m_TcpStream;
        private readonly TcpClient m_TcpClient;
        private readonly byte[] m_TcpBuffer;

        private Task m_ReceiverTask = null!;
        private CancellationTokenSource m_StoppingToken = null!;

        public HapClient(TcpClient tcpClient, ILogger<HapClient> logger)
        {
            m_TcpClient = tcpClient;
            m_Logger = logger;

            m_Socket = tcpClient.Client;
            m_TcpStream = tcpClient.GetStream();
            m_TcpBuffer = new byte[2048];
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
            while (!stoppingToken.IsCancellationRequested && m_TcpClient.Connected)
            {
                var bytesRead = await m_TcpStream.ReadAsync(m_TcpBuffer, stoppingToken);
                if (bytesRead == 0)
                {
                    break;
                }

                if (bytesRead == m_TcpBuffer.Length)
                {
                    // todo handle this case
                    throw new NotImplementedException();
                }

                m_Logger.LogInformation("Received TCP data {length}", bytesRead);
                m_Logger.LogTrace("TCP: {data}", BitConverter.ToString(m_TcpBuffer[0..bytesRead]));

                var plain = Encoding.UTF8.GetString(m_TcpBuffer[0..bytesRead]);
                var plainHeaders = plain.Split(Environment.NewLine);
                var where = plainHeaders[0].Split(' ');

                var method = where[0];
                var path = where[1];
                var version = where[2];

                var host = string.Empty;
                var contentLength = 0;
                var contentType = string.Empty;

                for (int i = 1; i < plainHeaders.Length; i++)
                {
                    var split = plainHeaders[i].Split(": ", 2);
                    if (split.Length == 2)
                    {
                        if (split[0] == "Host") host = split[1];
                        if (split[0] == "Content-Length") contentLength = int.Parse(split[1]);
                        if (split[0] == "Content-Type") contentType = split[1];
                    }
                }

                var content = m_TcpBuffer[(bytesRead - contentLength)..bytesRead];

                var response = (method, path) switch
                {
                    ("POST", "/pair-setup") => PairSetup(content),
                    _ => throw new NotImplementedException(),
                };

                // todo respond
            }

            m_Logger.LogInformation("Closing TCP client {remote}", m_Socket.RemoteEndPoint);
            m_TcpClient.Close();
        }

        private List<byte> PairSetup(ReadOnlySpan<byte> data)
        {
            var tlvs = TlvReader.ReadTlvs(data);
            var sequenceTlv = tlvs.FirstOrDefault(it => it.Tag == 6);
            var sequenceValue = 0;

            if (sequenceTlv != default && sequenceTlv.Length > 0)
            {
                sequenceValue = sequenceTlv.Value[0];
            }

            return sequenceValue switch
            {
                0 => PairSetup_M1(tlvs),
                1 => PairSetup_M2(tlvs),
                2 => PairSetup_M3(tlvs),
                3 => PairSetup_M4(tlvs),
                4 => PairSetup_M5(tlvs),
                _ => throw new NotImplementedException(),
            };
        }

        private List<byte> PairSetup_M1(Tlv[] tlvs)
        {
            throw new NotImplementedException();
        }

        private List<byte> PairSetup_M2(Tlv[] tlvs)
        {
            throw new NotImplementedException();
        }

        private List<byte> PairSetup_M3(Tlv[] tlvs)
        {
            throw new NotImplementedException();
        }

        private List<byte> PairSetup_M4(Tlv[] tlvs)
        {
            throw new NotImplementedException();
        }

        private List<byte> PairSetup_M5(Tlv[] tlvs)
        {
            throw new NotImplementedException();
        }


    }
}
