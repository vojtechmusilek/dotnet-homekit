﻿using System;
using System.Text;

namespace HomeKit.Hap
{
    internal static class HttpWriter
    {
        public static int WritePairingOk(Span<byte> buffer)
        {
            int position = 0;
            position += Encoding.UTF8.GetBytes("HTTP/1.1 200 OK\r\n", buffer[position..]);
            position += Encoding.UTF8.GetBytes("Server: Kestrel\r\n", buffer[position..]);
            position += Encoding.UTF8.GetBytes($"Date: {DateTime.UtcNow:r}\r\n", buffer[position..]);
            position += Encoding.UTF8.GetBytes($"Content-Type: application/pairing+tlv8\r\n", buffer[position..^0]);
            return position;
        }

        public static int WritePairingOkTest(Span<byte> buffer)
        {
            int position = 0;
            position += Encoding.UTF8.GetBytes("HTTP/1.1 200 OK\r\n", buffer[position..]);
            position += Encoding.UTF8.GetBytes($"Content-Type: application/pairing+tlv8\r\n", buffer[position..^0]);
            return position;
        }

        public static int WriteContent(Span<byte> buffer, ReadOnlySpan<byte> content)
        {
            int position = 0;
            position += Encoding.UTF8.GetBytes($"Content-Length: {content.Length}\r\n\r\n", buffer[position..]);
            content.CopyTo(buffer[position..]);
            position += content.Length;
            return position;
        }
    }
}
