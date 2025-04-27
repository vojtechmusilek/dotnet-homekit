using System;
using System.Collections.Generic;
using System.Numerics;
using System.Security.Cryptography;
using System.Text;

namespace HomeKit.Hap
{
    public class Srp6aServer
    {
        private static readonly int m_Modulus_N_Bits = 3072;

        private static readonly byte[] m_Modulus_N_Bytes = new byte[] {
            0xFF,0xFF,0xFF,0xFF,0xFF,0xFF,0xFF,0xFF,0xC9,0x0F,0xDA,0xA2,0x21,0x68,0xC2,0x34,0xC4,0xC6,0x62,0x8B,0x80,0xDC,0x1C,0xD1,
            0x29,0x02,0x4E,0x08,0x8A,0x67,0xCC,0x74,0x02,0x0B,0xBE,0xA6,0x3B,0x13,0x9B,0x22,0x51,0x4A,0x08,0x79,0x8E,0x34,0x04,0xDD,
            0xEF,0x95,0x19,0xB3,0xCD,0x3A,0x43,0x1B,0x30,0x2B,0x0A,0x6D,0xF2,0x5F,0x14,0x37,0x4F,0xE1,0x35,0x6D,0x6D,0x51,0xC2,0x45,
            0xE4,0x85,0xB5,0x76,0x62,0x5E,0x7E,0xC6,0xF4,0x4C,0x42,0xE9,0xA6,0x37,0xED,0x6B,0x0B,0xFF,0x5C,0xB6,0xF4,0x06,0xB7,0xED,
            0xEE,0x38,0x6B,0xFB,0x5A,0x89,0x9F,0xA5,0xAE,0x9F,0x24,0x11,0x7C,0x4B,0x1F,0xE6,0x49,0x28,0x66,0x51,0xEC,0xE4,0x5B,0x3D,
            0xC2,0x00,0x7C,0xB8,0xA1,0x63,0xBF,0x05,0x98,0xDA,0x48,0x36,0x1C,0x55,0xD3,0x9A,0x69,0x16,0x3F,0xA8,0xFD,0x24,0xCF,0x5F,
            0x83,0x65,0x5D,0x23,0xDC,0xA3,0xAD,0x96,0x1C,0x62,0xF3,0x56,0x20,0x85,0x52,0xBB,0x9E,0xD5,0x29,0x07,0x70,0x96,0x96,0x6D,
            0x67,0x0C,0x35,0x4E,0x4A,0xBC,0x98,0x04,0xF1,0x74,0x6C,0x08,0xCA,0x18,0x21,0x7C,0x32,0x90,0x5E,0x46,0x2E,0x36,0xCE,0x3B,
            0xE3,0x9E,0x77,0x2C,0x18,0x0E,0x86,0x03,0x9B,0x27,0x83,0xA2,0xEC,0x07,0xA2,0x8F,0xB5,0xC5,0x5D,0xF0,0x6F,0x4C,0x52,0xC9,
            0xDE,0x2B,0xCB,0xF6,0x95,0x58,0x17,0x18,0x39,0x95,0x49,0x7C,0xEA,0x95,0x6A,0xE5,0x15,0xD2,0x26,0x18,0x98,0xFA,0x05,0x10,
            0x15,0x72,0x8E,0x5A,0x8A,0xAA,0xC4,0x2D,0xAD,0x33,0x17,0x0D,0x04,0x50,0x7A,0x33,0xA8,0x55,0x21,0xAB,0xDF,0x1C,0xBA,0x64,
            0xEC,0xFB,0x85,0x04,0x58,0xDB,0xEF,0x0A,0x8A,0xEA,0x71,0x57,0x5D,0x06,0x0C,0x7D,0xB3,0x97,0x0F,0x85,0xA6,0xE1,0xE4,0xC7,
            0xAB,0xF5,0xAE,0x8C,0xDB,0x09,0x33,0xD7,0x1E,0x8C,0x94,0xE0,0x4A,0x25,0x61,0x9D,0xCE,0xE3,0xD2,0x26,0x1A,0xD2,0xEE,0x6B,
            0xF1,0x2F,0xFA,0x06,0xD9,0x8A,0x08,0x64,0xD8,0x76,0x02,0x73,0x3E,0xC8,0x6A,0x64,0x52,0x1F,0x2B,0x18,0x17,0x7B,0x20,0x0C,
            0xBB,0xE1,0x17,0x57,0x7A,0x61,0x5D,0x6C,0x77,0x09,0x88,0xC0,0xBA,0xD9,0x46,0xE2,0x08,0xE2,0x4F,0xA0,0x74,0xE5,0xAB,0x31,
            0x43,0xDB,0x5B,0xFC,0xE0,0xFD,0x10,0x8E,0x4B,0x82,0xD1,0x20,0xA9,0x3A,0xD2,0xCA,0xFF,0xFF,0xFF,0xFF,0xFF,0xFF,0xFF,0xFF,
        };
        private static readonly BigInteger m_Modulus_N = new(m_Modulus_N_Bytes, true, true);

        private static readonly byte[] m_Generator_g_Bytes = [5];
        private static readonly BigInteger m_Generator_g = new(m_Generator_g_Bytes);

        private byte[]? m_Salt_s;
        private byte[]? m_Username_I;
        private byte[]? m_ServerPublicEphemeral_B;
        private byte[]? m_SharedSecret_K;
        private byte[]? m_ClientPublicEphemeral_A;
        private BigInteger m_ServerSecretEphemeral_b;
        private BigInteger m_Verifier_v;

        public void SetSalt(ReadOnlySpan<byte> salt)
        {
            m_Salt_s = salt.ToArray();
        }

        public void SetUsernameAndPassword(string username, string password)
        {
            if (m_Salt_s is null) throw new Exception("wrong order");

            m_Username_I = Encoding.UTF8.GetBytes(username);
            var m_Password_p = Encoding.UTF8.GetBytes(password);

            m_Verifier_v = GetVerifier(m_Username_I, m_Password_p, m_Salt_s);
            m_ServerSecretEphemeral_b = GeneratePrivateKey();

            m_ServerPublicEphemeral_B = DerivePublicKey(m_Verifier_v, m_ServerSecretEphemeral_b).ToByteArray(true, true);
        }

        public void GeneratePublicKey(Span<byte> buffer)
        {
            m_ServerPublicEphemeral_B.CopyTo(buffer);
        }

        public void GetSharedSecret(Span<byte> buffer)
        {
            m_SharedSecret_K.CopyTo(buffer);
        }

        public void ComputeKey(byte[] A)
        {
            if (m_ServerPublicEphemeral_B is null) throw new Exception("wrong order");

            m_ClientPublicEphemeral_A = A;

            var sharedSecretUnhashed = DeriveSharedSecret(m_ClientPublicEphemeral_A, m_ServerPublicEphemeral_B, m_Verifier_v, m_ServerSecretEphemeral_b);

            m_SharedSecret_K = SHA512.HashData(sharedSecretUnhashed.ToByteArray(true, true));
        }

        public bool TryRespond(ReadOnlySpan<byte> M, Span<byte> response)
        {
            if (m_Username_I is null) throw new Exception("wrong order");
            if (m_Salt_s is null) throw new Exception("wrong order");
            if (m_ClientPublicEphemeral_A is null) throw new Exception("wrong order");
            if (m_ServerPublicEphemeral_B is null) throw new Exception("wrong order");
            if (m_SharedSecret_K is null) throw new Exception("wrong order");

            var calcM = GetM(m_Username_I, m_Salt_s, m_ClientPublicEphemeral_A, m_ServerPublicEphemeral_B, m_SharedSecret_K);

            if (Utils.SpansEqual(calcM, M))
            {
                var mBytes = Utils.MergeBytes(m_ClientPublicEphemeral_A, M.ToArray(), m_SharedSecret_K);
                SHA512.HashData(mBytes, response);
                return true;
            }
            else
            {
                return false;
            }
        }



        public static BigInteger DeriveSharedSecret(byte[] A, byte[] B, BigInteger v, BigInteger b)
        {
            var A_bigint = new BigInteger(A, true, true);
            var A_B_bytes = SHA512.HashData(Utils.MergeBytes(Pad(A), Pad(B)));
            var A_B = new BigInteger(A_B_bytes, true, true);
            var a_v_u_n = A_bigint * BigInteger.ModPow(v, A_B, m_Modulus_N);
            return BigInteger.ModPow(a_v_u_n, b, m_Modulus_N);
        }

        public static BigInteger DerivePublicKey(BigInteger v, BigInteger b)
        {
            var multiplierBytes = SHA512.HashData(Utils.MergeBytes(m_Modulus_N_Bytes, Pad(m_Generator_g_Bytes)));
            var multiplier = new BigInteger(multiplierBytes, true, true);
            return (multiplier * v + BigInteger.ModPow(m_Generator_g, b, m_Modulus_N)) % m_Modulus_N;
        }

        public static byte[] GetM(byte[] I, byte[] s, byte[] A, byte[] B, byte[] K)
        {
            var hN = SHA512.HashData(m_Modulus_N_Bytes);
            var hg = SHA512.HashData(m_Generator_g_Bytes);

            var buffer = new List<byte>();

            for (int i = 0; i < hN.Length; i++)
            {
                buffer.Add((byte)(hN[i] ^ hg[i]));
            }

            buffer.AddRange(SHA512.HashData(I));
            buffer.AddRange(s);
            buffer.AddRange(A);
            buffer.AddRange(B);
            buffer.AddRange(K);

            return SHA512.HashData(buffer.ToArray());
        }

        private static byte[] Pad(byte[] bytes)
        {
            var len = m_Modulus_N_Bits / 8;

            if (bytes.Length > len)
            {
                return bytes;
            }
            else
            {
                var padding = len - bytes.Length;
                var buffer = new List<byte>();

                for (int i = 0; i < padding; i++)
                {
                    buffer.Add(0);
                }

                return Utils.MergeBytes(buffer.ToArray(), bytes);
            }
        }

        public static BigInteger GetVerifier(byte[] I, byte[] p, byte[] s)
        {
            var I_p = SHA512.HashData(Utils.MergeBytes(I, Encoding.UTF8.GetBytes(":"), p));
            var s_I_p = SHA512.HashData(Utils.MergeBytes(s, I_p));
            var s_I_p_bi = new BigInteger(s_I_p, true, true);
            return BigInteger.ModPow(m_Generator_g, s_I_p_bi, m_Modulus_N);
        }

        private static BigInteger GeneratePrivateKey()
        {
            var buffer = new byte[32];
            Random.Shared.NextBytes(buffer);
            return new BigInteger(buffer, true, true);
        }
    }
}
