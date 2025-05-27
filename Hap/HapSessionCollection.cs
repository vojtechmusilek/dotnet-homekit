using System.Collections.Generic;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace HomeKit.Hap
{
    internal class HapSessionCollection
    {
        private readonly Dictionary<string, HapSession> m_HapSessions = new();

        internal async Task AddAndStart(HapSession session, IPEndPoint ipEndPoint, CancellationToken stoppingToken)
        {
            var ip = ipEndPoint.Address.ToString();
            if (m_HapSessions.TryGetValue(ip, out var existingSession))
            {
                await existingSession.StopAsync();
            }

            m_HapSessions[ip] = session;
            await session.StartAsync(stoppingToken);
        }

        internal async Task StopAsync()
        {
            foreach (var session in m_HapSessions)
            {
                await session.Value.StopAsync();
            }
        }
    }
}
