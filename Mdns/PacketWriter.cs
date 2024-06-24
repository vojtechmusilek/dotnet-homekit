using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace HomeKit.Mdns
{
    internal static class PacketWriter
    {
        public static byte[] WritePacket(Packet packet)
        {
            var buffer = new List<byte>();

            packet.Header.Questions = (ushort)(packet.Questions?.Length ?? 0);
            packet.Header.Answers = (ushort)(packet.Answers?.Length ?? 0);
            packet.Header.Authorities = (ushort)(packet.Authorities?.Length ?? 0);
            packet.Header.Additionals = (ushort)(packet.Additionals?.Length ?? 0);

            WriteUInt16(buffer, packet.Header.Id);
            WriteUInt16(buffer, packet.Header.Flags);
            WriteUInt16(buffer, packet.Header.Questions);
            WriteUInt16(buffer, packet.Header.Answers);
            WriteUInt16(buffer, packet.Header.Authorities);
            WriteUInt16(buffer, packet.Header.Additionals);

            for (int i = 0; i < packet.Header.Questions; i++)
            {
                WriteDomainNameUncompressed(buffer, packet.Questions![i].Name);
                WriteUInt16(buffer, packet.Questions[i].Type);
                WriteUInt16(buffer, packet.Questions[i].Class);
            }

            for (int i = 0; i < packet.Header.Answers; i++)
            {
                WritePacketRecord(buffer, packet.Answers![i]);
            }

            for (int i = 0; i < packet.Header.Authorities; i++)
            {
                WritePacketRecord(buffer, packet.Authorities![i]);
            }
            ;
            for (int i = 0; i < packet.Header.Additionals; i++)
            {
                WritePacketRecord(buffer, packet.Additionals![i]);
            }
            ;
            return buffer.ToArray();
        }

        private static void WriteUInt16(List<byte> buffer, ushort value)
        {
            buffer.Add((byte)(value >> 8));
            buffer.Add((byte)value);
        }

        private static void WriteUInt32(List<byte> buffer, uint value)
        {
            buffer.Add((byte)(value >> 24));
            buffer.Add((byte)(value >> 16));
            buffer.Add((byte)(value >> 8));
            buffer.Add((byte)value);
        }

        private static void WritePacketRecord(List<byte> buffer, PacketRecord packetRecord)
        {
            WriteDomainNameCompressed(buffer, packetRecord.Name);
            WriteUInt16(buffer, packetRecord.Type);
            WriteUInt16(buffer, packetRecord.Class);
            WriteUInt32(buffer, packetRecord.Ttl);

            int lengthPosition = buffer.Count;
            buffer.Add(0);
            buffer.Add(0);

            if (packetRecord.Data is PacketRecordData_PTR ptr)
            {
                WriteDomainNameCompressed(buffer, ptr.Name);
            }
            else if (packetRecord.Data is PacketRecordData_SRV srv)
            {
                WriteUInt16(buffer, srv.Priority);
                WriteUInt16(buffer, srv.Weight);
                WriteUInt16(buffer, srv.Port);
                WriteDomainNameUncompressed(buffer, srv.Target);
            }
            else if (packetRecord.Data is PacketRecordData_TXT txt)
            {
                foreach (var (key, val) in txt.KeyValuePairs)
                {
                    WriteString(buffer, key + "=" + val);
                }
            }
            else if (packetRecord.Data is PacketRecordData_A a)
            {
                buffer.AddRange(a.IpAddress.GetAddressBytes());
            }
            else if (packetRecord.Data is PacketRecordData_NSEC nsec)
            {
                WriteDomainNameUncompressed(buffer, nsec.NextName);
                WriteNsecTypes(buffer, nsec.IncludedTypes);
            }
            else
            {
                throw new NotImplementedException();
            }

            int recordDataLength = buffer.Count - lengthPosition - 2;
            if (recordDataLength > 0xFFFF)
            {
                throw new Exception("Record data is too long");
            }

            buffer[lengthPosition] = (byte)(recordDataLength >> 8);
            buffer[lengthPosition + 1] = (byte)recordDataLength;
        }

        private static void WriteString(List<byte> buffer, string value, int maxLength = int.MaxValue)
        {
            var bytes = Encoding.UTF8.GetBytes(value);
            if (bytes.Length > maxLength)
            {
                throw new Exception("String is too long");
            }

            buffer.Add((byte)bytes.Length);
            buffer.AddRange(bytes);
        }

        private static void WriteDomainNameUncompressed(List<byte> buffer, string value)
        {
            var split = value.Split('.', StringSplitOptions.RemoveEmptyEntries);

            foreach (var item in split)
            {
                WriteString(buffer, item, 63);
            }

            buffer.Add(0);
        }

        private static void WriteNsecTypes(List<byte> buffer, HashSet<ushort> types)
        {
            if (types.Count == 0)
            {
                throw new Exception("Cannot write zero types");
            }

            var map = new byte[256, 32];

            foreach (var type in types)
            {
                int n = type;
                if (n < 0 || n > 0xffff)
                {
                    throw new Exception("Invalid type");
                }

                int window = n >> 8;
                int index = n & 0xff;
                int slot = index >> 3;
                int bit = index & 7;

                map[window, slot] |= (byte)(1 << bit);
            }

            for (int window = 0; window < 256; ++window)
            {
                int length = 0;
                for (int slot = 0; slot < 32; ++slot)
                {
                    if (map[window, slot] != 0)
                    {
                        length = slot + 1;
                    }
                }

                if (length > 0)
                {
                    buffer.Add((byte)window);
                    buffer.Add((byte)length);
                    for (int slot = 0; slot < length; ++slot)
                    {
                        buffer.Add(map[window, slot]);
                    }
                }
            }
        }

        public static void WriteDomainNameCompressed(List<byte> buffer, string value)
        {
            var uncompressed = new List<byte>();
            WriteDomainNameUncompressed(uncompressed, value);

            int index = 0;
            while (uncompressed[index] != 0)
            {
                if (FindTail(CollectionsMarshal.AsSpan(buffer), CollectionsMarshal.AsSpan(uncompressed), index, out int position))
                {
                    buffer.Add((byte)(0xc0 | (position >> 8)));
                    buffer.Add((byte)position);
                    return;
                }

                buffer.Add(uncompressed[index]);
                for (int k = 1; k <= uncompressed[index]; ++k)
                {
                    buffer.Add(uncompressed[index + k]);
                }

                index += 1 + uncompressed[index];
            }

            buffer.Add(0);
        }

        /// <summary>https://datatracker.ietf.org/doc/html/rfc1035#section-4.1.4</summary>
        private static bool FindTail(ReadOnlySpan<byte> buffer, ReadOnlySpan<byte> serial, int index, out int position)
        {
            position = -1;

            int tailLength = serial.Length - index;
            if (tailLength < 3)
            {
                return false;
            }

            int limit = Math.Min(0x3fff, 1 + (buffer.Length - tailLength));

            for (position = 0; position < limit; ++position)
            {
                bool match = true;
                for (int offset = 0; match && (offset < tailLength); ++offset)
                {
                    if (buffer[position + offset] != serial[index + offset])
                    {
                        match = false;
                    }
                }

                if (match)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
