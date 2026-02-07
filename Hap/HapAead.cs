using System;
using System.Security.Cryptography;
using Microsoft.Extensions.Logging;

namespace HomeKit.Hap
{
    internal class HapAead
    {
        private const int m_MaxBlockLength = 1024;

        private readonly ILogger m_Logger;

        private ChaCha20Poly1305 m_RxKey = null!;
        private ChaCha20Poly1305 m_TxKey = null!;

        private ulong m_RxCount = 0;
        private ulong m_TxCount = 0;

        private bool m_RxEnabled = false;
        private bool m_TxEnabled = false;

        public bool Encrypted => m_RxEnabled;

        public HapAead(ILogger logger)
        {
            m_Logger = logger;
        }

        public void Enable(ReadOnlySpan<byte> sharedSecret)
        {
            Span<byte> hkdf = stackalloc byte[32];

            Crypto.HkdfSha512DeriveKey(hkdf, sharedSecret, "Control-Salt", "Control-Write-Encryption-Key");
            m_RxKey = new ChaCha20Poly1305(hkdf);

            Crypto.HkdfSha512DeriveKey(hkdf, sharedSecret, "Control-Salt", "Control-Read-Encryption-Key");
            m_TxKey = new ChaCha20Poly1305(hkdf);

            m_RxEnabled = true;
        }

        public int Encrypt(ReadOnlySpan<byte> data, Span<byte> buffer)
        {
            if (!m_TxEnabled)
            {
                return data.Length;
            }

            Span<byte> copy = stackalloc byte[data.Length];
            data.CopyTo(copy);

            var copyPosition = 0;
            var bufferPosition = 0;

            Span<byte> ciphertext = stackalloc byte[m_MaxBlockLength];
            Span<byte> tag = stackalloc byte[16];
            Span<byte> nonce = stackalloc byte[12];
            Span<byte> asocdata = stackalloc byte[2];

            while (copyPosition < copy.Length)
            {
                var blockLength = (ushort)Math.Min(copy.Length - copyPosition, m_MaxBlockLength);
                BitConverter.TryWriteBytes(asocdata, blockLength);
                BitConverter.TryWriteBytes(nonce[4..], m_TxCount++);
                var block = copy.Slice(copyPosition, blockLength);
                copyPosition += blockLength;

                try
                {
                    m_TxKey.Encrypt(nonce, block, ciphertext[..block.Length], tag, asocdata);
                }
                catch (Exception ex)
                {
                    m_Logger.LogError(ex, "Encryption failed");
                    return 0;
                }

                asocdata.CopyTo(buffer[bufferPosition..]);
                bufferPosition += asocdata.Length;

                ciphertext[..block.Length].CopyTo(buffer[bufferPosition..]);
                bufferPosition += block.Length;

                tag.CopyTo(buffer[bufferPosition..]);
                bufferPosition += tag.Length;
            }

            return bufferPosition;
        }

        public int Decrypt(ReadOnlySpan<byte> data, Span<byte> buffer)
        {
            if (!m_RxEnabled)
            {
                return data.Length;
            }

            m_TxEnabled = true;

            var dataPosition = 0;
            var bufferPosition = 0;

            Span<byte> nonce = stackalloc byte[12];

            while (dataPosition < data.Length)
            {
                var asocdata = data.Slice(dataPosition, 2);
                dataPosition += 2;

                BitConverter.TryWriteBytes(nonce[4..], m_RxCount++);

                var blockLength = BitConverter.ToUInt16(asocdata);

                var ciphertext = data.Slice(dataPosition, blockLength);
                dataPosition += blockLength;

                var tag = data.Slice(dataPosition, 16);
                dataPosition += 16;

                try
                {
                    m_RxKey.Decrypt(nonce, ciphertext, tag, buffer.Slice(bufferPosition, ciphertext.Length), asocdata);
                    bufferPosition += ciphertext.Length;
                }
                catch (Exception ex)
                {
                    m_Logger.LogError(ex, "Decryption failed");
                    return 0;
                }
            }

            return bufferPosition;
        }
    }
}
