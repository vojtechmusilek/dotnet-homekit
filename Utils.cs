using System;
using System.Buffers;
using System.Buffers.Text;
using System.Collections.Generic;
using System.Linq;
using System.Net.NetworkInformation;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using HomeKit.Resources;

namespace HomeKit
{
    public static class Utils
    {
        private const string m_AlphaNumChars = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ";

        public static readonly JsonSerializerOptions HapJsonOptions = new()
        {
            ReadCommentHandling = JsonCommentHandling.Skip,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        };

        public static readonly JsonSerializerOptions HapDefJsonOptions = new()
        {
            ReadCommentHandling = JsonCommentHandling.Skip,
            AllowTrailingCommas = true,
        };

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
            payload |= (int)category & (uint)0xFF;
            payload <<= 4;
            payload |= 2 & 0xF;
            payload <<= 27;
            payload |= int.Parse(pinCode.Replace("-", "")) & (uint)0x7FFFFFFF;

            var encodedPayload = ConvertToBase36(payload).PadLeft(9, '0');

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

        public static byte[] MergeBytes(params byte[][] bytes)
        {
            var length = bytes.Sum(it => it.Length);
            var result = new byte[length];

            var position = 0;
            for (int i = 0; i < bytes.Length; i++)
            {
                var tempBytes = bytes[i];
                Array.Copy(tempBytes, 0, result, position, tempBytes.Length);
                position += tempBytes.Length;
            }

            return result;
        }

        public static bool SpansEqual(ReadOnlySpan<byte> b1, ReadOnlySpan<byte> b2)
        {
            return b1.SequenceEqual(b2);
        }

        public static byte[] PadTlsNonce(this byte[] bytes, int totalLen = 12)
        {
            var len = bytes.Length;
            if (len >= totalLen)
            {
                return bytes;
            }

            var loss = totalLen - bytes.Length;
            var resultList = new List<byte>();
            for (int i = 0; i < loss; i++)
            {
                resultList.Add(0);
            }

            var result = MergeBytes(resultList.ToArray(), bytes);
            return result;
        }

        public static void WriteUtf8Bytes(Span<byte> buffer, ReadOnlySpan<char> value)
        {
            Encoding.UTF8.GetBytes(value, buffer);
        }

        public static void WriteUtf8BytesAlignedToRight(Span<byte> buffer, ReadOnlySpan<char> value)
        {
            var offset = buffer.Length - value.Length;
            Encoding.UTF8.GetBytes(value, buffer[offset..]);
        }

        public static string GetHapType(Guid uuid)
        {
            var uppercaseUuid = uuid.ToString().ToUpper();

            if (uppercaseUuid.EndsWith("0000-1000-8000-0026BB765291"))
            {
                return uppercaseUuid.Split('-')[0].TrimStart('0');
            }

            return uppercaseUuid;
        }

        public static Guid ReadUtf8Identifier(ReadOnlySpan<byte> identifier)
        {
            if (Utf8Parser.TryParse(identifier, out Guid guid, out int bytesConsumed))
            {
                return guid;
            }

            throw new Exception("Failed to read guid");
        }

        public static int WriteUtf8Identifier(Guid identifier, Span<byte> buffer)
        {
            if (Utf8Formatter.TryFormat(identifier, buffer, out int bytesWritten, new StandardFormat('D')))
            {
                return bytesWritten;
            }

            throw new Exception("Failed to write guid");
        }
    }
}
