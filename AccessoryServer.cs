using System;
using System.Collections.Generic;
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

        public static Dictionary<int, Characteristic> TemporaryCharList = new();

        public static byte[]? MainControlerIdentifier;
        public static byte[]? MainControlerLtPk;
        public static byte? MainControlerPermissions;

        public static Dictionary<Guid, byte[]> PairedClientPublicKeys = new();
        public static Dictionary<Guid, byte> PairedClientPermissions = new();

        public List<Accessory> Accessories { get; }

        public AccessoryServer()
        {
            Accessories = new();


        }

        // todo
        // Pin, Mac, Ip, ...
    }
}
