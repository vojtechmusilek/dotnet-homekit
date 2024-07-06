using System;
using System.Security.Cryptography;

namespace HomeKit
{
    internal static class Crypto
    {
        internal static bool DecryptEncryptedData(Span<byte> decryptedDataBuffer, ReadOnlySpan<byte> encryptedDataWithTag, ReadOnlySpan<byte> inputKeyingMaterial, string salt, string info, string nonce)
        {
            try
            {
                Span<byte> nonceEncoded = stackalloc byte[12];
                Utils.WriteUtf8BytesAlignedToRight(nonceEncoded, nonce);

                Span<byte> outputKeyingMaterial = stackalloc byte[32];
                HkdfSha512DeriveKey(outputKeyingMaterial, inputKeyingMaterial, salt, info);

                var chaCha20Poly1305 = new ChaCha20Poly1305(outputKeyingMaterial);
                chaCha20Poly1305.Decrypt(nonceEncoded, encryptedDataWithTag[..^16], encryptedDataWithTag[^16..], decryptedDataBuffer);

                return true;
            }
            catch
            {
                return false;
            }
        }

        internal static void HkdfSha512DeriveKey(Span<byte> outputKeyingMaterialBuffer, ReadOnlySpan<byte> inputKeyingMaterial, string salt, string info)
        {
            Span<byte> saltEncoded = stackalloc byte[salt.Length];
            Utils.WriteUtf8Bytes(saltEncoded, salt);

            Span<byte> infoEncoded = stackalloc byte[info.Length];
            Utils.WriteUtf8Bytes(infoEncoded, info);

            HKDF.DeriveKey(HashAlgorithmName.SHA512, inputKeyingMaterial, outputKeyingMaterialBuffer, saltEncoded, infoEncoded);
        }

        internal static void EncryptData(Span<byte> encryptedDataWithTagBuffer, ReadOnlySpan<byte> decryptedData, ReadOnlySpan<byte> inputKeyingMaterial, string salt, string info, string nonce)
        {
            Span<byte> saltEncoded = stackalloc byte[salt.Length];
            Utils.WriteUtf8Bytes(saltEncoded, salt);

            Span<byte> infoEncoded = stackalloc byte[info.Length];
            Utils.WriteUtf8Bytes(infoEncoded, info);

            Span<byte> nonceEncoded = stackalloc byte[12];
            Utils.WriteUtf8BytesAlignedToRight(nonceEncoded, nonce);

            Span<byte> outputKeyingMaterial = stackalloc byte[32];
            HKDF.DeriveKey(HashAlgorithmName.SHA512, inputKeyingMaterial, outputKeyingMaterial, saltEncoded, infoEncoded);

            var chaCha20Poly1305 = new ChaCha20Poly1305(outputKeyingMaterial);
            chaCha20Poly1305.Encrypt(nonceEncoded, decryptedData, encryptedDataWithTagBuffer[..^16], encryptedDataWithTagBuffer[^16..]);
        }
    }
}
