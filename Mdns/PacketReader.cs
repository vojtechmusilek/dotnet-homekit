using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace HomeKit.Mdns
{
    internal static class PacketReader
    {
        public static Packet ReadPacket(ReadOnlySpan<byte> data)
        {
            var packet = new Packet();

            int position = 0;

            packet.Header = new PacketHeader()
            {
                Id = ReadUInt16(data, ref position),
                Flags = ReadUInt16(data, ref position),
                Questions = ReadUInt16(data, ref position),
                Answers = ReadUInt16(data, ref position),
                Authorities = ReadUInt16(data, ref position),
                Additionals = ReadUInt16(data, ref position),
            };

            packet.Questions = new PacketQuestion[packet.Header.Questions];
            packet.Answers = new PacketRecord[packet.Header.Answers];
            packet.Authorities = new PacketRecord[packet.Header.Authorities];
            packet.Additionals = new PacketRecord[packet.Header.Additionals];

            for (int i = 0; i < packet.Header.Questions; i++)
            {
                packet.Questions[i] = new PacketQuestion()
                {
                    Name = ReadDomainName(data, ref position),
                    Type = ReadUInt16(data, ref position),
                    Class = ReadUInt16(data, ref position),
                };
            }

            for (int i = 0; i < packet.Header.Answers; i++)
            {
                packet.Answers[i] = ReadPacketRecord(data, ref position);
            }

            for (int i = 0; i < packet.Header.Authorities; i++)
            {
                packet.Authorities[i] = ReadPacketRecord(data, ref position);
            }

            for (int i = 0; i < packet.Header.Additionals; i++)
            {
                packet.Additionals[i] = ReadPacketRecord(data, ref position);
            }

            return packet;
        }

        private static PacketRecord ReadPacketRecord(ReadOnlySpan<byte> data, ref int position)
        {
            var name = ReadDomainName(data, ref position);
            var type = ReadUInt16(data, ref position);
            var @class = ReadUInt16(data, ref position);
            var ttl = ReadUInt32(data, ref position);

            var recordDataLength = ReadUInt16(data, ref position);
            IPacketRecordData recordData = new UnknownPacketRecordData();
            var positionBefore = position;

            if (type == PacketRecordData_PTR.Type)
            {
                recordData = new PacketRecordData_PTR()
                {
                    Name = ReadDomainName(data, ref position)
                };
            }
            else
            {
                ReadBytes(data, recordDataLength, ref position);
            }

            if (position - positionBefore != recordDataLength)
            {
                throw new InvalidOperationException("Packet record data length does not match");
            }

            return new PacketRecord()
            {
                Name = name,
                Type = type,
                Class = @class,
                Ttl = ttl,
                DataLength = recordDataLength,
                Data = recordData,
            };
        }

        private static ReadOnlySpan<byte> ReadBytes(ReadOnlySpan<byte> data, int count, ref int position)
        {
            position += count;
            return data[(position - count)..position];
        }

        private static byte ReadByte(ReadOnlySpan<byte> data, ref int position)
        {
            position++;
            return data[position - 1];
        }

        private static ushort ReadUInt16(ReadOnlySpan<byte> data, ref int position)
        {
            return BinaryPrimitives.ReadUInt16BigEndian(ReadBytes(data, 2, ref position));
        }

        private static uint ReadUInt32(ReadOnlySpan<byte> data, ref int position)
        {
            return BinaryPrimitives.ReadUInt32BigEndian(ReadBytes(data, 4, ref position));
        }

        private static string ReadDomainName(ReadOnlySpan<byte> data, ref int position)
        {
            var bytes = new List<byte>();

            int length = ReadByte(data, ref position);
            while (length > 0)
            {
                var isReference = (length & 0xC0) == 0xC0;
                var isRawUtf8 = (length & 0xC0) == 0;

                if (isReference)
                {
                    int referencePosition = ((length & 0x3F) << 8) | ReadByte(data, ref position);
                    if (referencePosition < 0 || referencePosition >= position - 2)
                    {
                        throw new NullReferenceException("Invalid reference");
                    }

                    var referenceLength = ReadByte(data, ref referencePosition);
                    bytes.AddRange(ReadBytes(data, referenceLength, ref referencePosition));
                    bytes.Add((byte)'.');
                    return Encoding.UTF8.GetString(bytes.ToArray());
                }
                else if (isRawUtf8)
                {
                    bytes.AddRange(ReadBytes(data, length, ref position));
                }
                else
                {
                    throw new NotSupportedException("Domain name compression");
                }

                bytes.Add((byte)'.');
                length = ReadByte(data, ref position);
            }

            var name = Encoding.UTF8.GetString(CollectionsMarshal.AsSpan(bytes));
            if (name == "") return ".";
            return name;
        }
    }
}
