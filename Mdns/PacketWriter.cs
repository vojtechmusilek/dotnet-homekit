using System;
using System.Collections.Generic;
using System.Linq;
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
                WriteDomainNameUncompressed(buffer, packet.Questions[i].Name);
                WriteUInt16(buffer, packet.Questions[i].Type);
                WriteUInt16(buffer, packet.Questions[i].Class);
            }

            for (int i = 0; i < packet.Header.Answers; i++)
            {
                WritePacketRecord(buffer, packet.Answers[i]);
            }

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
            WriteDomainNameUncompressed(buffer, packetRecord.Name);
            WriteUInt16(buffer, packetRecord.Type);
            WriteUInt16(buffer, packetRecord.Class);
            WriteUInt32(buffer, packetRecord.Ttl);

            int lengthPosition = buffer.Count;
            buffer.Add(0);
            buffer.Add(0);

            if (packetRecord.Data is PacketRecordData_PTR ptr)
            {
                WriteDomainNameUncompressed(buffer, ptr.Name);
            }
            else if (packetRecord.Data is PacketRecordData_SRV srv)
            {
                WriteUInt16(buffer, srv.Priority);
                WriteUInt16(buffer, srv.Weight);
                WriteUInt32(buffer, srv.Port);
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
                buffer.AddRange(a.IpAddress.Split('.').Select(byte.Parse));
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
    }
}
