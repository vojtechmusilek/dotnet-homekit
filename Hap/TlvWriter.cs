using System;

namespace HomeKit.Hap
{
    internal static class TlvWriter
    {
        public static int WriteTlv(Span<byte> buffer, TlvType type, TlvError value)
        {
            return WriteTlv(buffer, type, (byte)value);
        }

        public static int WriteTlv(Span<byte> buffer, TlvType type, byte value)
        {
            buffer[0] = (byte)type;
            buffer[1] = 1;
            buffer[2] = value;
            return 3;
        }

        public static int WriteTlv(Span<byte> buffer, TlvType type, ReadOnlySpan<byte> value)
        {
            var position = 0;
            var length = value.Length;
            if (length <= 255)
            {
                buffer[position++] = (byte)type;
                buffer[position++] = (byte)value.Length;
                value.CopyTo(buffer[position..]);
                position += value.Length;
                return position;
            }

            for (int i = 0; i < length / 255; i++)
            {
                buffer[position++] = (byte)type;
                buffer[position++] = 255;
                value[(i * 255)..((i + 1) * 255)].CopyTo(buffer[position..]);
                position += 255;
            }

            var rest = length % 255;
            buffer[position++] = (byte)type;
            buffer[position++] = (byte)rest;
            value[(length - rest)..length].CopyTo(buffer[position..]);
            position += rest;
            return position;
        }
    }
}
