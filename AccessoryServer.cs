using System.Threading;

namespace HomeKit
{
    internal class AccessoryServer
    {
        // todo not static
        private static int m_InstanceCounter;

        // todo not static
        public static int GenerateInstanceId()
        {
            return Interlocked.Increment(ref m_InstanceCounter);
        }

        // todo
        // Accessories
        // Pin, Mac, Ip, ...
    }
}
