using System;

namespace HomeKit.Hap
{
    internal static class TlvReader
    {
        public static int ReadValue(Span<byte> buffer, TlvType type, ReadOnlySpan<byte> data)
        {
            var dataPosition = 0;
            var bufferPosition = 0;

            while (dataPosition < data.Length)
            {
                var currentType = (TlvType)data[dataPosition++];
                var currentLength = data[dataPosition++];

                if (currentType == type)
                {
                    data.Slice(dataPosition, currentLength).CopyTo(buffer[bufferPosition..]);
                    bufferPosition += currentLength;
                }

                dataPosition += currentLength;
            }

            return bufferPosition;
        }

        public static byte? ReadValue(TlvType type, ReadOnlySpan<byte> data)
        {
            var dataPosition = 0;

            while (dataPosition < data.Length)
            {
                var currentType = (TlvType)data[dataPosition++];
                var currentLength = data[dataPosition++];

                if (currentType == type && currentLength == 1)
                {
                    return data[dataPosition++];
                }

                dataPosition += currentLength;
            }

            return null;
        }
    }
}
