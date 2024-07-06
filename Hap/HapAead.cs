using System;
using System.Security.Cryptography;

namespace HomeKit.Hap
{
    internal class HapAead
    {
        private const int m_MaxBlockLength = 1024;

        private ChaCha20Poly1305? m_Key;
        private ulong m_Count = 0;

        public HapAead()
        {

        }

        public void Enable(ReadOnlySpan<byte> sharedSecret, string info)
        {
            Span<byte> hkdf = stackalloc byte[32];

            Crypto.HkdfSha512DeriveKey(hkdf, sharedSecret, "Control-Salt", info);
            m_Key = new ChaCha20Poly1305(hkdf);
        }

        public int Encrypt(ReadOnlySpan<byte> data, Span<byte> buffer)
        {
            if (m_Key is null)
            {
                return buffer.Length;
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
                BitConverter.TryWriteBytes(nonce[4..], m_Count++);
                var block = copy.Slice(copyPosition, blockLength);
                copyPosition += blockLength;

                m_Key.Encrypt(nonce, block, ciphertext[..block.Length], tag, asocdata);

                asocdata.CopyTo(buffer[bufferPosition..]);
                bufferPosition += asocdata.Length;

                ciphertext[..block.Length].CopyTo(buffer[bufferPosition..]);
                bufferPosition += block.Length;

                tag.CopyTo(buffer[bufferPosition..]);
                bufferPosition += tag.Length;
            }

            return bufferPosition;
        }

        public int Decrypt(Span<byte> data)
        {
            if (m_Key is null)
            {
                return data.Length;
            }

            throw new NotImplementedException();
        }
    }
}
