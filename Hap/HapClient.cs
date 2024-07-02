using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Ed25519;
using Microsoft.Extensions.Logging;
using X25519;

namespace HomeKit.Hap
{
    /// <summary>
    /// HomeKit Accessory Protocol Client
    /// </summary>
    internal class HapClient
    {
        private readonly ILogger m_Logger;
        private readonly Socket m_Socket;
        private readonly NetworkStream m_TcpStream;
        private readonly TcpClient m_TcpClient;
        private readonly Srp6aServer m_Srp;
        private readonly string m_PinCode;
        private readonly string m_MacAddress;
        private readonly byte[] m_ReadBuffer;
        private readonly byte[] m_WriteBuffer;

        private Task m_ReceiverTask = null!;
        private CancellationTokenSource m_StoppingToken = null!;

        public HapClient(TcpClient tcpClient, string pinCode, string macAddress, ILogger<HapClient> logger)
        {
            m_TcpClient = tcpClient;
            m_PinCode = pinCode;
            m_MacAddress = macAddress;
            m_Logger = logger;

            m_Socket = tcpClient.Client;
            m_TcpStream = tcpClient.GetStream();

            m_ReadBuffer = new byte[2048];
            m_WriteBuffer = new byte[2048];

            m_Srp = new Srp6aServer();
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            m_StoppingToken = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            m_ReceiverTask = ReceiverTask(m_StoppingToken.Token);
            return Task.CompletedTask;
        }

        public async Task StopAsync()
        {
            await m_StoppingToken.CancelAsync();
            await m_ReceiverTask;
        }

        private async Task ReceiverTask(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested && m_TcpClient.Connected)
            {
                var requestLength = await m_TcpStream.ReadAsync(m_ReadBuffer, stoppingToken);
                m_Logger.LogInformation("TCP Rx {length}", requestLength);

                if (requestLength == 0)
                {
                    break;
                }

                if (requestLength == m_ReadBuffer.Length)
                {
                    // todo handle this case
                    throw new NotImplementedException();
                }

                //#if DEBUG
                //                m_Logger.LogDebug("TCP REQ:\n{data}", Encoding.UTF8.GetString(m_ReadBuffer.AsSpan(0, requestLength)));
                //#endif

                int responseLength = ProcessRequest(requestLength);
                if (responseLength == 0)
                {
                    continue;
                }

                //#if DEBUG
                //                m_Logger.LogDebug("TCP RES:\n{data}", Encoding.UTF8.GetString(m_WriteBuffer.AsSpan(0, responseLength)));
                //#endif

                m_Logger.LogInformation("TCP Tx {length}", responseLength);
                await m_TcpStream.WriteAsync(m_WriteBuffer.AsMemory(0, responseLength), stoppingToken);
            }

            m_Logger.LogInformation("Closing TCP client {remote}", m_Socket.RemoteEndPoint);
            m_TcpClient.Close();
        }

        private int ProcessRequest(int length)
        {
            var data = m_ReadBuffer.AsSpan()[0..length];
            if (Accessory.Temporary_Ready)
            {
                var decrypted = DecryptRequest(data);
                data = decrypted.AsSpan();
            }

            var plain = Encoding.UTF8.GetString(data);
            var plainHeaders = plain.Split(Environment.NewLine);
            var where = plainHeaders[0].Split(' ');

            var method = where[0];
            var path = where[1];
            var version = where[2];

            var host = string.Empty;
            var contentLength = 0;
            var contentType = string.Empty;

            for (int i = 1; i < plainHeaders.Length; i++)
            {
                var split = plainHeaders[i].Split(": ", 2);
                if (split.Length == 2)
                {
                    if (split[0] == "Host") host = split[1];
                    if (split[0] == "Content-Length") contentLength = int.Parse(split[1]);
                    if (split[0] == "Content-Type") contentType = split[1];
                }
            }

            Span<byte> content = Span<byte>.Empty;
            if (contentLength > 0)
            {
                content = data.Slice(length - contentLength, contentLength);
            }

            var tx = m_WriteBuffer.AsSpan();

            return (method, path) switch
            {
                ("POST", "/pair-setup") => PairSetup(content, tx),
                ("POST", "/pair-verify") => PairVerify(content, tx),
                ("GET", "/accessories") => throw new NotImplementedException(),
                _ => throw new NotImplementedException(),
            };
        }

        private byte[] DecryptRequest(ReadOnlySpan<byte> data)
        {
            int tagLen = 16;
            int lengthLen = 2;
            int minLen = 1;
            int minBlockLen = lengthLen + tagLen + minLen;

            int count = 0;
            var hkdf = HKDF.DeriveKey(
                new HashAlgorithmName(nameof(SHA512)), Accessory.Temporary_SharedSecret_pvM1_pvM3_reqHapDecrypt, 32,
                Encoding.UTF8.GetBytes("Control-Salt"),
                Encoding.UTF8.GetBytes("Control-Write-Encryption-Key")
            );
            var chacha = new ChaCha20Poly1305(hkdf);

            // todo stackalloc
            var result = new List<byte>();
            var dataList = data.ToArray().ToList();

            while (dataList.Count > minBlockLen)
            {
                var blockLengthBytes = dataList.Take(2).ToList();
                var blockSize = BitConverter.ToUInt16(blockLengthBytes.ToArray());

                var blockSizeWithLength = lengthLen + blockSize + tagLen;
                if (dataList.Count < blockSizeWithLength)
                {
                    return result.ToArray();
                }

                dataList = dataList.Skip(lengthLen).ToList();
                var dataSize = blockSize + tagLen;
                var nonce = BitConverter.GetBytes((ulong)count).PadTlsNonce();

                var encryptedDataWithTag = dataList.Take(dataSize).ToArray();
                var encryptedData = encryptedDataWithTag[..^16];
                byte[] tag = encryptedDataWithTag[^16..];

                var decryptedData = new byte[encryptedData.Length];

                try
                {
                    chacha.Decrypt(nonce, encryptedData, tag, decryptedData, blockLengthBytes.ToArray());
                }
                catch (Exception excc)
                {
                    m_Logger.LogError(excc, "hap decrypt fail");
                    throw;
                }

                result.AddRange(decryptedData);
                count += 1;
                dataList = dataList.Skip(dataSize).ToList();
            }

            return result.ToArray();
        }

        private int PairSetup(ReadOnlySpan<byte> rx, Span<byte> tx)
        {
            var state = TlvReader.ReadValue(TlvType.State, rx);
            return state switch
            {
                1 => PairSetup_M1(rx, tx),
                3 => PairSetup_M3(rx, tx),
                5 => PairSetup_M5(rx, tx),
                _ => throw new NotImplementedException(),
            };
        }

        private int PairSetup_M1(ReadOnlySpan<byte> rx, Span<byte> tx)
        {
            m_Logger.LogInformation("Pair setup M1->M2");

            /// 5.6.2 - 1
            // todo if paired respond unavailable

            /// 5.6.2 - 2
            // todo max tries

            /// 5.6.2 - 3
            // todo busy

            /// 5.6.2 - 4,5,6,7
            Span<byte> salt = stackalloc byte[16];
            Random.Shared.NextBytes(salt);

            m_Srp.SetSalt(salt);
            m_Srp.SetUsernameAndPassword("Pair-Setup", m_PinCode);

            /// 5.6.2 - 9
            Span<byte> accessorySrpPublicKey = stackalloc byte[384];
            m_Srp.GeneratePublicKey(accessorySrpPublicKey);

            /// 5.6.2 - 10
            Span<byte> content = stackalloc byte[409];
            var contentLength = 0;
            contentLength += TlvWriter.WriteTlv(content[contentLength..], TlvType.State, 2);
            contentLength += TlvWriter.WriteTlv(content[contentLength..], TlvType.Salt, salt);
            contentLength += TlvWriter.WriteTlv(content[contentLength..], TlvType.PublicKey, accessorySrpPublicKey);

            return WriteHttpOkResponse(tx, content);
        }

        private int PairSetup_M3(ReadOnlySpan<byte> rx, Span<byte> tx)
        {
            m_Logger.LogInformation("Pair setup M3->M4");

            /// 5.6.4 - 1
            Span<byte> publicKey = stackalloc byte[384];
            TlvReader.ReadValue(publicKey, TlvType.PublicKey, rx);

            // caches shared secret..
            m_Srp.ComputeKey(publicKey.ToArray());

            /// 5.6.4 - 2, 3
            Span<byte> passwordProof = stackalloc byte[64];
            TlvReader.ReadValue(passwordProof, TlvType.Proof, rx);

            var accessoryProof = m_Srp.Respond(passwordProof.ToArray());

            /// 5.6.4 - 4, 5
            Span<byte> content = stackalloc byte[69];
            var contentLength = 0;
            contentLength += TlvWriter.WriteTlv(content[contentLength..], TlvType.State, 4);
            contentLength += TlvWriter.WriteTlv(content[contentLength..], TlvType.Proof, accessoryProof);

            return WriteHttpOkResponse(tx, content);
        }

        private int PairSetup_M5(ReadOnlySpan<byte> rx, Span<byte> tx)
        {
            m_Logger.LogInformation("Pair setup M5->M6");

            /// M5 Verification

            Span<byte> sharedSecret = stackalloc byte[64];
            m_Srp.GetSharedSecret(sharedSecret);

            /// 5.6.6.1 - 1,2
            Span<byte> encryptedDataWithTag = stackalloc byte[154]; // ok?
            TlvReader.ReadValue(encryptedDataWithTag, TlvType.EncryptedData, rx);

            Span<byte> decryptedData = stackalloc byte[encryptedDataWithTag.Length - 16];
            bool decrypted = DecryptEncryptedData(
                decryptedData, encryptedDataWithTag, sharedSecret,
                "Pair-Setup-Encrypt-Salt", "Pair-Setup-Encrypt-Info", "PS-Msg05"
            );
            if (!decrypted)
            {

            }

            /// 5.6.6.1 - 3
            Span<byte> iosDeviceX = stackalloc byte[32];
            HkdfSha512DeriveKey(iosDeviceX, sharedSecret, "Pair-Setup-Controller-Sign-Salt", "Pair-Setup-Controller-Sign-Info");

            /// 5.6.6.1 - 4
            Span<byte> iosDevicePairingId = stackalloc byte[36]; // ok?
            TlvReader.ReadValue(iosDevicePairingId, TlvType.Identifier, decryptedData);

            Span<byte> iosDeviceLtPk = stackalloc byte[32]; // ok?
            TlvReader.ReadValue(iosDeviceLtPk, TlvType.PublicKey, decryptedData);

            Span<byte> iosDeviceInfo = stackalloc byte[100];
            iosDeviceX.CopyTo(iosDeviceInfo[0..]);
            iosDevicePairingId.CopyTo(iosDeviceInfo[32..]);
            iosDeviceLtPk.CopyTo(iosDeviceInfo[68..]);

            /// 5.6.6.1 - 5
            Span<byte> signature = stackalloc byte[64]; // ok?
            TlvReader.ReadValue(signature, TlvType.Signature, decryptedData);

            var valid = Signer.Validate(signature, iosDeviceInfo, iosDeviceLtPk);
            if (!valid)
            {
                // todo handle
            }

            /// 5.6.6.1 - 6
            // todo better
            Accessory.Temporary_IosDeviceLtpk_psM5_pvM3 = iosDeviceLtPk.ToArray();

            /// M6 Response Generation

            /// 5.6.6.2 - 1
            var accessoryLtSk = Signer.GeneratePrivateKey();
            var accessoryLtPk = accessoryLtSk.ExtractPublicKey();
            Accessory.Temporary_AccessoryLtSk_psM5_pvM1 = accessoryLtSk.ToArray();
            Accessory.Temporary_AccessoryLtPk_psM5_pvM1 = accessoryLtPk.ToArray();

            /// 5.6.6.2 - 2
            Span<byte> accessoryX = stackalloc byte[32];
            HkdfSha512DeriveKey(accessoryX, sharedSecret, "Pair-Setup-Accessory-Sign-Salt", "Pair-Setup-Accessory-Sign-Info");

            var mac = Encoding.UTF8.GetBytes(m_MacAddress);

            /// 5.6.6.2 - 3
            Span<byte> accessoryPairingId = stackalloc byte[17]; // ok?
            Utils.WriteUtf8Bytes(accessoryPairingId, m_MacAddress);

            Span<byte> accessoryInfo = stackalloc byte[81]; // ok?
            accessoryX.CopyTo(accessoryInfo[0..]);
            accessoryPairingId.CopyTo(accessoryInfo[32..]);
            accessoryLtPk.CopyTo(accessoryInfo[49..]);

            /// 5.6.6.2 - 4
            var accessorySignature = Signer.Sign(accessoryInfo, accessoryLtSk, accessoryLtPk); // todo

            /// 5.6.6.2 - 5
            Span<byte> subTlv = stackalloc byte[119];
            var subTlvPosition = 0;
            subTlvPosition += TlvWriter.WriteTlv(subTlv[subTlvPosition..], TlvType.Identifier, accessoryPairingId);
            subTlvPosition += TlvWriter.WriteTlv(subTlv[subTlvPosition..], TlvType.PublicKey, accessoryLtPk);
            subTlvPosition += TlvWriter.WriteTlv(subTlv[subTlvPosition..], TlvType.Signature, accessorySignature);

            /// 5.6.6.2 - 6
            Span<byte> encryptedDataWithTag_tx = stackalloc byte[135];
            EncryptData(
                encryptedDataWithTag_tx, subTlv, sharedSecret,
                "Pair-Setup-Encrypt-Salt", "Pair-Setup-Encrypt-Info", "PS-Msg06"
            );

            /// 5.6.6.2 - 7
            Span<byte> content = stackalloc byte[140];
            var contentPosition = 0;
            contentPosition += TlvWriter.WriteTlv(content[contentPosition..], TlvType.State, 6);
            contentPosition += TlvWriter.WriteTlv(content[contentPosition..], TlvType.EncryptedData, encryptedDataWithTag_tx);

            return WriteHttpOkResponse(tx, content);
        }

        private int PairVerify(ReadOnlySpan<byte> rx, Span<byte> tx)
        {
            var state = TlvReader.ReadValue(TlvType.State, rx);
            return state switch
            {
                1 => PairVerify_M1(rx, tx),
                3 => PairVerify_M3(rx, tx),
                _ => throw new InvalidOperationException(),
            };
        }

        private int PairVerify_M1(ReadOnlySpan<byte> rx, Span<byte> tx)
        {
            m_Logger.LogInformation("Pair verify M1->M2");

            /// 5.7.2 - 1
            var accessoryCurve = X25519KeyAgreement.GenerateKeyPair(); // pk 32 sk 32 todo 
            Accessory.Temporary_AccessoryCurvePk_pvM1_pvM3 = accessoryCurve.PublicKey;

            /// 5.7.2 - 2
            Span<byte> iosDeviceCurvePK = stackalloc byte[32];
            TlvReader.ReadValue(iosDeviceCurvePK, TlvType.PublicKey, rx);

            var sharedSecret = X25519KeyAgreement.Agreement(accessoryCurve.PrivateKey, iosDeviceCurvePK.ToArray()); // todo
            Accessory.Temporary_SharedSecret_pvM1_pvM3_reqHapDecrypt = sharedSecret; // 32
            Accessory.Temporary_IosDeviceCurvePk_pvM1_pvM3 = iosDeviceCurvePK.ToArray(); // 32

            /// 5.7.2 - 3
            Span<byte> accessoryPairingId = stackalloc byte[17];
            Utils.WriteUtf8Bytes(accessoryPairingId, m_MacAddress);

            Span<byte> accessoryInfo = stackalloc byte[81];
            accessoryCurve.PublicKey.CopyTo(accessoryInfo[0..]);
            accessoryPairingId.CopyTo(accessoryInfo[32..]);
            iosDeviceCurvePK.CopyTo(accessoryInfo[49..]);

            /// 5.7.2 - 4
            var accessoryLtSk = Accessory.Temporary_AccessoryLtSk_psM5_pvM1; // 32
            var accessoryLtPk = Accessory.Temporary_AccessoryLtPk_psM5_pvM1; // 32
            var accessorySignature = Signer.Sign(accessoryInfo, accessoryLtSk, accessoryLtPk); // 64

            /// 5.7.2 - 5
            Span<byte> subTlv = stackalloc byte[85];
            var subTlvPosition = 0;
            subTlvPosition += TlvWriter.WriteTlv(subTlv[subTlvPosition..], TlvType.Identifier, accessoryPairingId);
            subTlvPosition += TlvWriter.WriteTlv(subTlv[subTlvPosition..], TlvType.Signature, accessorySignature);

            /// 5.7.2 - 6, 7
            Span<byte> encryptedDataWithTag = stackalloc byte[101];
            EncryptData(
                encryptedDataWithTag, subTlv, Accessory.Temporary_SharedSecret_pvM1_pvM3_reqHapDecrypt,
                "Pair-Verify-Encrypt-Salt", "Pair-Verify-Encrypt-Info", "PV-Msg02"
            );

            /// 5.7.2 - 8
            Span<byte> content = stackalloc byte[140];
            var contentPosition = 0;
            contentPosition += TlvWriter.WriteTlv(content[contentPosition..], TlvType.State, 2);
            contentPosition += TlvWriter.WriteTlv(content[contentPosition..], TlvType.PublicKey, accessoryCurve.PublicKey);
            contentPosition += TlvWriter.WriteTlv(content[contentPosition..], TlvType.EncryptedData, encryptedDataWithTag);

            return WriteHttpOkResponse(tx, content);
        }

        private int PairVerify_M3(ReadOnlySpan<byte> rx, Span<byte> tx)
        {
            m_Logger.LogInformation("Pair verify M3->M4");

            /// 5.7.4 - 1, 2
            Span<byte> encryptedDataWithTag = stackalloc byte[120];
            TlvReader.ReadValue(encryptedDataWithTag, TlvType.EncryptedData, rx);

            Span<byte> decryptedData = stackalloc byte[encryptedDataWithTag.Length - 16];
            bool decrypted = DecryptEncryptedData(
                decryptedData, encryptedDataWithTag, Accessory.Temporary_SharedSecret_pvM1_pvM3_reqHapDecrypt,
                "Pair-Verify-Encrypt-Salt", "Pair-Verify-Encrypt-Info", "PV-Msg03"
            );

            if (!decrypted)
            {
                return WriteAuthenticationError(tx, 4);
            }

            /// 5.7.4 - 3
            Span<byte> iosDevicePairingId = stackalloc byte[36];
            TlvReader.ReadValue(iosDevicePairingId, TlvType.Identifier, decryptedData);

            // todo
            //Use the iOS deviceʼs Pairing Identifier, iOSDevicePairingID, to look up the iOS deviceʼs long-term pub
            //lic key, iOSDeviceLTPK, in its list of paired controllers
            var iosDeviceLTPK = Accessory.Temporary_IosDeviceLtpk_psM5_pvM3;
            bool found = true;
            if (!found)
            {
                return WriteAuthenticationError(tx, 4);
            }

            /// 5.7.4 - 4
            Span<byte> iosDeviceSignature = stackalloc byte[64];
            TlvReader.ReadValue(iosDeviceSignature, TlvType.Signature, decryptedData);

            Span<byte> iosDeviceInfo = stackalloc byte[100];
            Accessory.Temporary_IosDeviceCurvePk_pvM1_pvM3.CopyTo(iosDeviceInfo[0..]);
            iosDevicePairingId.CopyTo(iosDeviceInfo[32..]);
            Accessory.Temporary_AccessoryCurvePk_pvM1_pvM3.CopyTo(iosDeviceInfo[68..]);

            //var iosDeviceInfo = Utils.MergeBytes(Accessory.TemporaryIosDeviceCurvePK, iosDevicePairingId.ToArray(), Accessory.TemporaryAccessoryCurvePK);

            bool valid = Signer.Validate(iosDeviceSignature, iosDeviceInfo, iosDeviceLTPK);
            if (!valid)
            {
                return WriteAuthenticationError(tx, 4);
            }

            /// 5.7.4 - 5
            // todo better
            Accessory.Temporary_Ready = true;

            Span<byte> content = stackalloc byte[3];
            TlvWriter.WriteTlv(content, TlvType.State, 4);

            return WriteHttpOkResponse(tx, content);
        }

        private static int WriteAuthenticationError(Span<byte> httpBuffer, byte pairingState)
        {
            Span<byte> content = stackalloc byte[6];

            var contentLength = 0;
            contentLength += TlvWriter.WriteTlv(content[contentLength..], TlvType.Error, TlvError.Authentication);
            contentLength += TlvWriter.WriteTlv(content[contentLength..], TlvType.State, 4);

            return WriteHttpOkResponse(httpBuffer, content);
        }

        private static int WriteHttpOkResponse(Span<byte> httpBuffer, ReadOnlySpan<byte> content)
        {
            var httpLength = 0;
            httpLength += HttpWriter.WritePairingOkTest(httpBuffer[httpLength..]);
            httpLength += HttpWriter.WriteContent(httpBuffer[httpLength..], content[..content.Length]);
            return httpLength;
        }

        private bool DecryptEncryptedData(
            Span<byte> decryptedDataBuffer, ReadOnlySpan<byte> encryptedDataWithTag,
            ReadOnlySpan<byte> inputKeyingMaterial, string salt, string info, string nonce
        )
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

        private void HkdfSha512DeriveKey(Span<byte> outputKeyingMaterialBuffer, ReadOnlySpan<byte> inputKeyingMaterial, string salt, string info)
        {
            Span<byte> saltEncoded = stackalloc byte[salt.Length];
            Utils.WriteUtf8Bytes(saltEncoded, salt);

            Span<byte> infoEncoded = stackalloc byte[info.Length];
            Utils.WriteUtf8Bytes(infoEncoded, info);

            HKDF.DeriveKey(HashAlgorithmName.SHA512, inputKeyingMaterial, outputKeyingMaterialBuffer, saltEncoded, infoEncoded);
        }

        private void EncryptData(Span<byte> encryptedDataWithTagBuffer, ReadOnlySpan<byte> decryptedData, ReadOnlySpan<byte> inputKeyingMaterial, string salt, string info, string nonce)
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
