using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace HomeKit.Mdns
{
    internal static class PacketReader
    {
        public static Packet ReadPacket(byte[] data)
        {
            var packet = new Packet()
            {
                Questions = new()
            };

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

            for (int i = 0; i < packet.Header.Questions; i++)
            {
                packet.Questions.Add(new PacketQuestion()
                {
                    Name = ReadDomainName(data, ref position),
                    Type = ReadUInt16(data, ref position),
                    Class = ReadUInt16(data, ref position),
                });
            }

            return packet;
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

        internal static string ReadDomainName(ReadOnlySpan<byte> data, ref int position)
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
