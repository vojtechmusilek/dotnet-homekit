using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
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
        private readonly string m_PinCode;
        private readonly string m_MacAddress;

        private readonly Srp6aServer m_SrpServer = new();

        private readonly byte[] m_ReadBuffer = new byte[2048];
        private readonly byte[] m_WriteBuffer = new byte[2048];

        private readonly byte[] m_AccessoryCurvePk = new byte[32];
        private readonly byte[] m_IosDeviceCurvePk = new byte[32];
        private readonly byte[] m_SharedSecret = new byte[32];

        private bool m_AeadRxEnabled = false;
        private bool m_AeadTxEnabled = false;

        private int m_AeadRxCount = 0;
        private int m_AeadTxCount = 0;

        private ChaCha20Poly1305? m_AeadRxKey;
        private ChaCha20Poly1305? m_AeadTxKey;

        private readonly string m_RemoteIp;

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

            m_RemoteIp = m_Socket.RemoteEndPoint?.ToString() ?? "uknown";
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
                var requestLength = await m_TcpStream.ReadAtLeastAsync(m_ReadBuffer, 1, false, stoppingToken);
                m_Logger.LogInformation("TCP Rx {length}", requestLength);

                if (requestLength == 0)
                {
                    break;
                }

                if (requestLength == m_ReadBuffer.Length)
                {
                    throw new NotImplementedException("Read buffer overflow");
                }

                var encryptionOff = Encoding.UTF8.GetString(m_ReadBuffer)[0] is 'H' or 'P' or 'G';


                //#if DEBUG
                //                if (encryptionOff)
                //                {
                //                    m_Logger.LogDebug("TCP REQ:\n{data}", Encoding.UTF8.GetString(m_ReadBuffer.AsSpan(0, requestLength)));
                //                }
                //#endif

                try
                {
                    int responseLength = ProcessRequest(requestLength);
                    if (responseLength == 0)
                    {
                        continue;
                    }

                    //#if DEBUG
                    //                if (encryptionOff)
                    //                {
                    //                    m_Logger.LogDebug("TCP RES:\n{data}", Encoding.UTF8.GetString(m_WriteBuffer.AsSpan(0, responseLength)));
                    //                }
                    //#endif

                    m_Logger.LogInformation("TCP Tx {length}", responseLength);
                    await m_TcpStream.WriteAsync(m_WriteBuffer.AsMemory(0, responseLength), stoppingToken);
                }
                catch (Exception ex)
                {
                    m_Logger.LogError(ex, "Failed to process request");
                }
            }

            m_Logger.LogInformation("Closing HAP client {remote}", m_Socket.RemoteEndPoint);
            m_TcpClient.Close();
        }

        private int ProcessRequest(int length)
        {
            var data = m_ReadBuffer.AsSpan()[0..length];

            if (m_AeadRxEnabled)
            {
                m_AeadTxEnabled = true;

                var decrypted = DecryptRequest(data);
                data = decrypted.AsSpan();
                length = decrypted.Length;

                //#if DEBUG
                //                m_Logger.LogDebug("TCP REQ DECRYPTED:\n{data}", Encoding.UTF8.GetString(data));
                //#endif
            }

            var plain = Encoding.UTF8.GetString(data);
            var plainHeaders = plain.Split(Environment.NewLine);
            var where = plainHeaders[0].Split(' ');

            var method = where[0];
            var path = where[1];
            var version = where[2];

            var query = string.Empty;
            if (path.Contains('?'))
            {
                var split = path.Split('?');
                if (split.Length == 2)
                {
                    path = split[0];
                    query = split[1];
                }
            }

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

            m_Logger.LogInformation("{remote} -> {method}, {path} {query}", m_RemoteIp, method, path, query);

            var txLen = (method, path) switch
            {
                ("POST", "/pair-setup") => PairSetup(content, tx),
                ("POST", "/pair-verify") => PairVerify(content, tx),
                ("POST", "/pairings") => Pairings(content, tx),
                ("GET", "/accessories") => GetAccessories(content, tx),
                ("GET", "/characteristics") => GetCharacteristics(content, tx, query),
                ("PUT", "/characteristics") => PutCharacteristics(content, tx),
                _ => throw new NotImplementedException($"Method {method}, {path}, {query}"),
            };

            if (m_AeadTxEnabled)
            {
                //#if DEBUG
                //                m_Logger.LogDebug("TCP RES DECRYPTED:\n{data}", Encoding.UTF8.GetString(m_WriteBuffer.AsSpan(0, txLen)));
                //#endif

                var encrypted = EncryptResponse(m_WriteBuffer.AsSpan(0, txLen));
                var newlen = encrypted.Length;
                encrypted.CopyTo(m_WriteBuffer, 0);
                txLen = newlen;
            }

            return txLen;
        }

        private byte[] DecryptRequest(ReadOnlySpan<byte> data)
        {
            // todo cleanup

            int tagLen = 16;
            int lengthLen = 2;
            int minLen = 1;
            int minBlockLen = lengthLen + tagLen + minLen;

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
                var nonce = BitConverter.GetBytes((ulong)m_AeadRxCount).PadTlsNonce();

                var encryptedDataWithTag = dataList.Take(dataSize).ToArray();
                var encryptedData = encryptedDataWithTag[..^16];
                byte[] tag = encryptedDataWithTag[^16..];

                var decryptedData = new byte[encryptedData.Length];

                try
                {
                    m_AeadRxKey!.Decrypt(nonce, encryptedData, tag, decryptedData, blockLengthBytes.ToArray());
                }
                catch (Exception excc)
                {
                    m_Logger.LogError(excc, "hap decrypt fail");
                    throw;
                }

                result.AddRange(decryptedData);
                m_AeadRxCount += 1;
                dataList = dataList.Skip(dataSize).ToList();
            }

            return result.ToArray();
        }

        private byte[] EncryptResponse(ReadOnlySpan<byte> data)
        {
            // todo cleanup

            var result = new List<byte>();
            var offset = 0;
            var total = data.Length;
            var maxBlockLength = 1024;

            while (offset < total)
            {
                var length = Math.Min(total - offset, maxBlockLength);
                var lengthBytes = BitConverter.GetBytes((ushort)length);
                var block = data[offset..(offset + length)];
                var nonce = BitConverter.GetBytes((ulong)m_AeadTxCount).PadTlsNonce();
                var encryped = new byte[block.Length];
                var tag = new byte[16];

                m_AeadTxKey!.Encrypt(nonce, block, encryped, tag, lengthBytes);

                offset += length;
                m_AeadTxCount += 1;
                result.AddRange(lengthBytes);
                result.AddRange(encryped);
                result.AddRange(tag);
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
            var paired = false;
            if (paired)
            {
                return WriteError(tx, TlvError.Unavailable, 2);
            }

            /// 5.6.2 - 2
            // todo max tries
            var maxt = false;
            if (maxt)
            {
                return WriteError(tx, TlvError.MaxTries, 2);
            }

            /// 5.6.2 - 3
            // todo busy
            var busy = false;
            if (busy)
            {
                return WriteError(tx, TlvError.Busy, 2);
            }

            /// 5.6.2 - 4,5,6,7
            Span<byte> salt = stackalloc byte[16];
            Random.Shared.NextBytes(salt);

            m_SrpServer.SetSalt(salt);
            m_SrpServer.SetUsernameAndPassword("Pair-Setup", m_PinCode);

            /// 5.6.2 - 9
            Span<byte> accessorySrpPublicKey = stackalloc byte[384];
            m_SrpServer.GeneratePublicKey(accessorySrpPublicKey);

            /// 5.6.2 - 10
            Span<byte> content = stackalloc byte[409];
            var contentLength = 0;
            contentLength += TlvWriter.WriteTlv(content[contentLength..], TlvType.State, 2);
            contentLength += TlvWriter.WriteTlv(content[contentLength..], TlvType.Salt, salt);
            contentLength += TlvWriter.WriteTlv(content[contentLength..], TlvType.PublicKey, accessorySrpPublicKey);

            return WritePairingContent(tx, content);
        }

        private int PairSetup_M3(ReadOnlySpan<byte> rx, Span<byte> tx)
        {
            m_Logger.LogInformation("Pair setup M3->M4");

            /// 5.6.4 - 1
            Span<byte> publicKey = stackalloc byte[384];
            TlvReader.ReadValue(TlvType.PublicKey, rx, publicKey);

            // caches shared secret..
            m_SrpServer.ComputeKey(publicKey.ToArray());

            /// 5.6.4 - 2, 3
            Span<byte> passwordProof = stackalloc byte[64];
            TlvReader.ReadValue(TlvType.Proof, rx, passwordProof);

            var accessoryProof = m_SrpServer.Respond(passwordProof.ToArray());

            /// 5.6.4 - 4, 5
            Span<byte> content = stackalloc byte[69];
            var contentLength = 0;
            contentLength += TlvWriter.WriteTlv(content[contentLength..], TlvType.State, 4);
            contentLength += TlvWriter.WriteTlv(content[contentLength..], TlvType.Proof, accessoryProof);

            return WritePairingContent(tx, content);
        }

        private int PairSetup_M5(ReadOnlySpan<byte> rx, Span<byte> tx)
        {
            m_Logger.LogInformation("Pair setup M5->M6");

            /// M5 Verification

            Span<byte> sharedSecret = stackalloc byte[64];
            m_SrpServer.GetSharedSecret(sharedSecret);

            /// 5.6.6.1 - 1,2
            Span<byte> encryptedDataWithTag = stackalloc byte[154];
            TlvReader.ReadValue(TlvType.EncryptedData, rx, encryptedDataWithTag);

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
            Span<byte> iosDevicePairingId = stackalloc byte[36];
            TlvReader.ReadValue(TlvType.Identifier, decryptedData, iosDevicePairingId);

            Span<byte> iosDeviceLtPk = stackalloc byte[32];
            TlvReader.ReadValue(TlvType.PublicKey, decryptedData, iosDeviceLtPk);

            Span<byte> iosDeviceInfo = stackalloc byte[100];
            iosDeviceX.CopyTo(iosDeviceInfo[0..]);
            iosDevicePairingId.CopyTo(iosDeviceInfo[32..]);
            iosDeviceLtPk.CopyTo(iosDeviceInfo[68..]);

            /// 5.6.6.1 - 5
            Span<byte> signature = stackalloc byte[64];
            TlvReader.ReadValue(TlvType.Signature, decryptedData, signature);

            var valid = Signer.Validate(signature, iosDeviceInfo, iosDeviceLtPk);
            if (!valid)
            {
                // todo handle
            }

            /// 5.6.6.1 - 6
            if (AccessoryServer.MainControlerLtPk is not null)
            {
                // todo this should not happen
                throw new NotImplementedException();
            }

            AccessoryServer.MainControlerLtPk = iosDeviceLtPk.ToArray();
            AccessoryServer.MainControlerIdentifier = iosDevicePairingId.ToArray();
            AccessoryServer.MainControlerPermissions = 1;

            var guid = Utils.ReadUtf8Identifier(iosDevicePairingId);
            AccessoryServer.PairedClientPublicKeys[guid] = AccessoryServer.MainControlerLtPk;
            AccessoryServer.PairedClientPermissions[guid] = 1;

            /// M6 Response Generation

            /// 5.6.6.2 - 1
            var accessoryLtSk = Signer.GeneratePrivateKey();
            var accessoryLtPk = accessoryLtSk.ExtractPublicKey();
            Accessory.AccessoryLtSk = accessoryLtSk.ToArray();
            Accessory.AccessoryLtPk = accessoryLtPk.ToArray();

            /// 5.6.6.2 - 2
            Span<byte> accessoryX = stackalloc byte[32];
            HkdfSha512DeriveKey(accessoryX, sharedSecret, "Pair-Setup-Accessory-Sign-Salt", "Pair-Setup-Accessory-Sign-Info");

            var mac = Encoding.UTF8.GetBytes(m_MacAddress);

            /// 5.6.6.2 - 3
            Span<byte> accessoryPairingId = stackalloc byte[17];
            Utils.WriteUtf8Bytes(accessoryPairingId, m_MacAddress);

            Span<byte> accessoryInfo = stackalloc byte[81];
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

            return WritePairingContent(tx, content);
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
            accessoryCurve.PublicKey.AsSpan().CopyTo(m_AccessoryCurvePk);

            /// 5.7.2 - 2
            Span<byte> iosDeviceCurvePK = stackalloc byte[32];
            TlvReader.ReadValue(TlvType.PublicKey, rx, iosDeviceCurvePK);

            var sharedSecret = X25519KeyAgreement.Agreement(accessoryCurve.PrivateKey, iosDeviceCurvePK.ToArray()); // todo
            sharedSecret.AsSpan().CopyTo(m_SharedSecret);
            iosDeviceCurvePK.CopyTo(m_IosDeviceCurvePk);

            /// 5.7.2 - 3
            Span<byte> accessoryPairingId = stackalloc byte[17];
            Utils.WriteUtf8Bytes(accessoryPairingId, m_MacAddress);

            Span<byte> accessoryInfo = stackalloc byte[81];
            accessoryCurve.PublicKey.CopyTo(accessoryInfo[0..]);
            accessoryPairingId.CopyTo(accessoryInfo[32..]);
            iosDeviceCurvePK.CopyTo(accessoryInfo[49..]);

            /// 5.7.2 - 4
            var accessorySignature = Signer.Sign(accessoryInfo, Accessory.AccessoryLtSk, Accessory.AccessoryLtPk); // 64

            /// 5.7.2 - 5
            Span<byte> subTlv = stackalloc byte[85];
            var subTlvPosition = 0;
            subTlvPosition += TlvWriter.WriteTlv(subTlv[subTlvPosition..], TlvType.Identifier, accessoryPairingId);
            subTlvPosition += TlvWriter.WriteTlv(subTlv[subTlvPosition..], TlvType.Signature, accessorySignature);

            /// 5.7.2 - 6, 7
            Span<byte> encryptedDataWithTag = stackalloc byte[101];
            EncryptData(
                encryptedDataWithTag, subTlv, m_SharedSecret,
                "Pair-Verify-Encrypt-Salt", "Pair-Verify-Encrypt-Info", "PV-Msg02"
            );

            /// 5.7.2 - 8
            Span<byte> content = stackalloc byte[140];
            var contentPosition = 0;
            contentPosition += TlvWriter.WriteTlv(content[contentPosition..], TlvType.State, 2);
            contentPosition += TlvWriter.WriteTlv(content[contentPosition..], TlvType.PublicKey, accessoryCurve.PublicKey);
            contentPosition += TlvWriter.WriteTlv(content[contentPosition..], TlvType.EncryptedData, encryptedDataWithTag);

            return WritePairingContent(tx, content);
        }

        private int PairVerify_M3(ReadOnlySpan<byte> rx, Span<byte> tx)
        {
            m_Logger.LogInformation("Pair verify M3->M4");

            /// 5.7.4 - 1, 2
            Span<byte> encryptedDataWithTag = stackalloc byte[120];
            TlvReader.ReadValue(TlvType.EncryptedData, rx, encryptedDataWithTag);

            Span<byte> decryptedData = stackalloc byte[encryptedDataWithTag.Length - 16];
            bool decrypted = DecryptEncryptedData(
                decryptedData, encryptedDataWithTag, m_SharedSecret,
                "Pair-Verify-Encrypt-Salt", "Pair-Verify-Encrypt-Info", "PV-Msg03"
            );

            if (!decrypted)
            {
                return WriteError(tx, TlvError.Authentication, 4);
            }

            /// 5.7.4 - 3
            Span<byte> iosDevicePairingId = stackalloc byte[36];
            TlvReader.ReadValue(TlvType.Identifier, decryptedData, iosDevicePairingId);

            var iosDevicePairingGuid = Utils.ReadUtf8Identifier(iosDevicePairingId);
            if (!AccessoryServer.PairedClientPublicKeys.TryGetValue(iosDevicePairingGuid, out var iosDeviceLTPK))
            {
                return WriteError(tx, TlvError.Authentication, 4);
            }

            /// 5.7.4 - 4
            Span<byte> iosDeviceSignature = stackalloc byte[64];
            TlvReader.ReadValue(TlvType.Signature, decryptedData, iosDeviceSignature);

            Span<byte> iosDeviceInfo = stackalloc byte[100];
            m_IosDeviceCurvePk.CopyTo(iosDeviceInfo[0..]);
            iosDevicePairingId.CopyTo(iosDeviceInfo[32..]);
            m_AccessoryCurvePk.CopyTo(iosDeviceInfo[68..]);

            bool valid = Signer.Validate(iosDeviceSignature, iosDeviceInfo, iosDeviceLTPK);
            if (!valid)
            {
                return WriteError(tx, TlvError.Authentication, 4);
            }

            /// 5.7.4 - 5
            EnableAead();

            return WritePairingState(tx, 4);
        }

        private int Pairings(ReadOnlySpan<byte> rx, Span<byte> tx)
        {
            var state = TlvReader.ReadValue(TlvType.State, rx);
            if (state != 1) throw new NotImplementedException();

            var method = TlvReader.ReadValue(TlvType.Method, rx);
            if (method is null) throw new MissingFieldException("Method");

            return (TlvMethod)method.Value switch
            {
                TlvMethod.AddPairing => AddPairing(rx, tx),
                TlvMethod.RemovePairing => RemovePairing(rx, tx),
                TlvMethod.ListPairings => ListPairings(rx, tx),
                _ => throw new InvalidOperationException(),
            };
        }

        private int AddPairing(ReadOnlySpan<byte> rx, Span<byte> tx)
        {
            /// 5.10.2 - 1
            // todo

            /// 5.10.2 - 2
            // todo

            /// 5.10.2 - 3, 4
            Span<byte> additionalControllerPairingIdentifier = stackalloc byte[36];
            var acpi = TlvReader.ReadValue(TlvType.Identifier, rx, additionalControllerPairingIdentifier);

            Span<byte> additionalControllerLtPk = stackalloc byte[32];
            TlvReader.ReadValue(TlvType.PublicKey, rx, additionalControllerLtPk);

            var additionalControllerPermissions = TlvReader.ReadValue(TlvType.Permissions, rx);
            if (additionalControllerPermissions is null) throw new MissingFieldException("Permissions");

            var acpiGuid = Utils.ReadUtf8Identifier(additionalControllerPairingIdentifier);

            if (AccessoryServer.PairedClientPublicKeys.TryGetValue(acpiGuid, out byte[]? value))
            {
                if (!value.AsSpan().SequenceEqual(additionalControllerLtPk))
                {
                    return WriteError(tx, TlvError.Unknown, 2);
                }
                else
                {
                    AccessoryServer.PairedClientPermissions[acpiGuid] = additionalControllerPermissions.Value;
                }
            }
            else
            {
                // todo check kTLVError_MaxPeers

                AccessoryServer.PairedClientPublicKeys[acpiGuid] = additionalControllerLtPk.ToArray();
                AccessoryServer.PairedClientPermissions[acpiGuid] = additionalControllerPermissions.Value;
            }

            /// 5.10.2 - 5
            return WritePairingState(tx, 2);

            /// 5.10.2 - 6
            // todo - check if sending using right encryption
        }

        private int RemovePairing(ReadOnlySpan<byte> rx, Span<byte> tx)
        {
            /// 5.11.2 - 1
            // todo

            /// 5.11.2 - 2
            // todo

            /// 5.11.2 - 3
            Span<byte> removedControllerPairingIdentifier = stackalloc byte[36];
            var acpi = TlvReader.ReadValue(TlvType.Identifier, rx, removedControllerPairingIdentifier);
            var acpiGuid = Utils.ReadUtf8Identifier(removedControllerPairingIdentifier);

            AccessoryServer.PairedClientPublicKeys.Remove(acpiGuid);

            /// 5.11.2 - 4
            return WritePairingState(tx, 2);

            /// 5.11.2 - 5
            // todo

            /// 5.11.2 - 6
            // todo

            /// 5.11.2 - 7
            // todo
        }

        private int ListPairings(ReadOnlySpan<byte> rx, Span<byte> tx)
        {
            /// 5.12.2 - 1
            // todo

            /// 5.12.2 - 2
            // todo

            /// 5.12.2 - 3
            var controllerCount = AccessoryServer.PairedClientPublicKeys.Count - 1;

            Span<byte> content = stackalloc byte[3 + 75 + ((2 + 75) * controllerCount)];
            var contentPosition = 0;

            // 3
            contentPosition += TlvWriter.WriteTlv(content[contentPosition..], TlvType.State, 2);

            if (AccessoryServer.MainControlerPermissions is null)
            {
                throw new NotImplementedException();
            }

            // 75
            contentPosition += TlvWriter.WriteTlv(content[contentPosition..], TlvType.Identifier, AccessoryServer.MainControlerIdentifier);
            contentPosition += TlvWriter.WriteTlv(content[contentPosition..], TlvType.PublicKey, AccessoryServer.MainControlerLtPk);
            contentPosition += TlvWriter.WriteTlv(content[contentPosition..], TlvType.Permissions, AccessoryServer.MainControlerPermissions.Value);

            Span<byte> additionalIdentifier = stackalloc byte[36];

            var keys = AccessoryServer.PairedClientPublicKeys.Keys.ToArray();
            for (int i = 0; i < controllerCount; i++)
            {
                var key = keys[i];
                Utils.WriteUtf8Identifier(keys[i], additionalIdentifier);
                if (additionalIdentifier.SequenceEqual(AccessoryServer.MainControlerIdentifier))
                {
                    continue;
                }

                // 2
                content[contentPosition++] = (byte)TlvType.Separator;
                content[contentPosition++] = 0x00;

                // 75
                contentPosition += TlvWriter.WriteTlv(content[contentPosition..], TlvType.Identifier, additionalIdentifier);
                contentPosition += TlvWriter.WriteTlv(content[contentPosition..], TlvType.PublicKey, AccessoryServer.PairedClientPublicKeys[key]);
                contentPosition += TlvWriter.WriteTlv(content[contentPosition..], TlvType.Permissions, AccessoryServer.PairedClientPermissions[key]);
            }

            /// 5.12.2 - 4
            return WriteHapContent(tx, content);
        }

        private int GetAccessories(ReadOnlySpan<byte> rx, Span<byte> tx)
        {
            // tmp 
            var accessoryServer = new AccessoryServer();
            accessoryServer.Accessories.Add(Accessory.Temporary_Instance);

            var json = JsonSerializer.Serialize(accessoryServer, Utils.HapJsonOptions);
            var jsonbytes = Encoding.UTF8.GetBytes(json);

            return WriteHapContent(tx, jsonbytes);
        }

        private int PutCharacteristics(ReadOnlySpan<byte> rx, Span<byte> tx)
        {
            /// 6.7.2
            var request = JsonSerializer.Deserialize<CharacteristicWriteRequest>(rx, Utils.HapJsonOptions);

            // todo process and if error reply error

            return HttpWriter.WriteNoContent(tx);
        }

        private int GetCharacteristics(ReadOnlySpan<byte> rx, Span<byte> tx, string query)
        {
            // "id=1.7,1.5"
            var queries = query.Split('&');

            var ids = queries.FirstOrDefault(x => x.Split('=')[0] == "id")?.Split('=')[1];
            if (ids is null)
            {
                throw new NotImplementedException();
            }

            var requests = ids.Split(',');

            var characteristics = new CharacteristicRead[requests.Length];

            for (int i = 0; i < requests.Length; i++)
            {
                var split = requests[i].Split('.');
                if (split.Length == 2)
                {
                    var aid = int.Parse(split[0]);
                    var iid = int.Parse(split[1]);

                    var characteristic = AccessoryServer.TemporaryCharList[iid];
                    var val = characteristic.Value;

                    characteristics[i] = new CharacteristicRead()
                    {
                        Aid = aid,
                        Iid = iid,
                        Value = val,
                        //Status = 0,
                    };
                }
            }

            var response = new CharacteristicReadResponse()
            {
                Characteristics = characteristics
            };

            var json = JsonSerializer.Serialize(response, Utils.HapJsonOptions);
            var jsonbytes = Encoding.UTF8.GetBytes(json);

            return WriteHapContent(tx, jsonbytes);
        }



        private void EnableAead()
        {
            Span<byte> hkdf = stackalloc byte[32];

            HkdfSha512DeriveKey(hkdf, m_SharedSecret, "Control-Salt", "Control-Write-Encryption-Key");
            m_AeadRxKey = new ChaCha20Poly1305(hkdf);

            HkdfSha512DeriveKey(hkdf, m_SharedSecret, "Control-Salt", "Control-Read-Encryption-Key");
            m_AeadTxKey = new ChaCha20Poly1305(hkdf);

            m_AeadRxEnabled = true;
        }

        private static int WriteError(Span<byte> httpBuffer, TlvError error, byte state)
        {
            Span<byte> content = stackalloc byte[6];

            var contentLength = 0;
            contentLength += TlvWriter.WriteTlv(content[contentLength..], TlvType.Error, error);
            contentLength += TlvWriter.WriteTlv(content[contentLength..], TlvType.State, state);

            return WritePairingContent(httpBuffer, content);
        }

        private static int WritePairingContent(Span<byte> httpBuffer, ReadOnlySpan<byte> content)
        {
            var httpLength = 0;
            httpLength += HttpWriter.WritePairingOk(httpBuffer[httpLength..]);
            httpLength += HttpWriter.WriteContent(httpBuffer[httpLength..], content[..content.Length]);
            return httpLength;
        }

        private static int WritePairingState(Span<byte> httpBuffer, byte state)
        {
            Span<byte> content = stackalloc byte[3];
            TlvWriter.WriteTlv(content, TlvType.State, state);

            return WritePairingContent(httpBuffer, content);
        }

        private static int WriteHapContent(Span<byte> httpBuffer, ReadOnlySpan<byte> content)
        {
            var httpLength = 0;
            httpLength += HttpWriter.WriteHapOk(httpBuffer[httpLength..]);
            httpLength += HttpWriter.WriteContent(httpBuffer[httpLength..], content[..content.Length]);
            return httpLength;
        }

        private static bool DecryptEncryptedData(Span<byte> decryptedDataBuffer, ReadOnlySpan<byte> encryptedDataWithTag, ReadOnlySpan<byte> inputKeyingMaterial, string salt, string info, string nonce)
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

        private static void HkdfSha512DeriveKey(Span<byte> outputKeyingMaterialBuffer, ReadOnlySpan<byte> inputKeyingMaterial, string salt, string info)
        {
            Span<byte> saltEncoded = stackalloc byte[salt.Length];
            Utils.WriteUtf8Bytes(saltEncoded, salt);

            Span<byte> infoEncoded = stackalloc byte[info.Length];
            Utils.WriteUtf8Bytes(infoEncoded, info);

            HKDF.DeriveKey(HashAlgorithmName.SHA512, inputKeyingMaterial, outputKeyingMaterialBuffer, saltEncoded, infoEncoded);
        }

        private static void EncryptData(Span<byte> encryptedDataWithTagBuffer, ReadOnlySpan<byte> decryptedData, ReadOnlySpan<byte> inputKeyingMaterial, string salt, string info, string nonce)
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
