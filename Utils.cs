using System;
using System.Linq;
using System.Net.NetworkInformation;
using System.Text;
using HomeKit.Resources;

namespace HomeKit
{
    public static class Utils
    {
        private const string m_AlphaNumChars = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ";

        public static string ConvertToBase36(long value)
        {
            var sb = new StringBuilder();
            while (value > 0)
            {
                sb.Insert(0, m_AlphaNumChars[(int)(value % 36)]);
                value /= 36;
            }
            return sb.ToString();
        }

        public static string GenerateSetupId()
        {
            var sb = new StringBuilder();
            for (int i = 0; i <= 3; i++)
            {
                var index = Random.Shared.Next(0, m_AlphaNumChars.Length - 1);
                sb.Append(m_AlphaNumChars[index]);
            }
            return sb.ToString();
        }

        public static string GenerateXhmUri(Category category, string pinCode, string setupId)
        {
            long payload = 0;

            payload |= 0 & 0x7;
            payload <<= 4;
            payload |= 0 & 0xF;
            payload <<= 8;
            payload |= (int)category & 0xFF; // todo try (uint)0xFF
            payload <<= 4;
            payload |= 2 & 0xF;
            payload <<= 27;
            payload |= int.Parse(pinCode.Replace("-", "")) & 0x7FFFFFFF; // todo try (uint)0x7FFFFFFF

            // todo inline ConvertToBase36
            var encodedPayload = ConvertToBase36(payload);
            encodedPayload = encodedPayload.PadLeft(9, '0');

            return $"X-HM://{encodedPayload}{setupId}";
        }

        public static string GenerateMacAddress()
        {
            // todo validate if i need to set locally administered and unicast bits
            var buffer = new byte[6];
            Random.Shared.NextBytes(buffer);
            return string.Join(':', buffer.Select(x => x.ToString("X2")));
        }

        public static NetworkInterface[] GetMulticastNetworkInterfaces()
        {
            return NetworkInterface.GetAllNetworkInterfaces().Where(ni =>
            {
                if (!ni.SupportsMulticast) return false;
                if (ni.NetworkInterfaceType == NetworkInterfaceType.Loopback) return false;
                if (ni.OperationalStatus != OperationalStatus.Up) return false;

                // todo check for index?
                var props = ni.GetIPProperties().GetIPv4Properties();

                return true;
            }).ToArray();
        }

        public static ushort SetBits(ushort oldValue, int position, int length, ushort newValue)
        {
            if (length <= 0 || position >= 16)
            {
                return oldValue;
            }

            int mask = (2 << (length - 1)) - 1;
            oldValue &= (ushort)~(mask << position);
            oldValue |= (ushort)((newValue & mask) << position);
            return oldValue;
        }

        public static ushort GetBits(ushort oldValue, int position, int length)
        {
            if (length <= 0 || position >= 16)
            {
                return 0;
            }

            int mask = (2 << (length - 1)) - 1;
            return (ushort)((oldValue >> position) & mask);
        }
    }
}
