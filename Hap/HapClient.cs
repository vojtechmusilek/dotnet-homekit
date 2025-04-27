using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
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

        private readonly AccessoryServer m_AccessoryServer;
        private readonly Srp6aServer m_SrpServer = new();
        private readonly HapAead m_Aead;

        private readonly byte[] m_ReadBuffer = new byte[1024];
        private readonly byte[] m_WriteBuffer = new byte[4096];
        private readonly byte[] m_EventBuffer = new byte[1024];

        private readonly byte[] m_AccessoryCurvePk = new byte[32];
        private readonly byte[] m_IosDeviceCurvePk = new byte[32];
        private readonly byte[] m_SharedSecret = new byte[32];

        private readonly string m_RemoteIp;

        private PairedClient? m_PairedClient;

        private Task m_ReceiverTask = null!;
        private CancellationTokenSource m_StoppingToken = null!;

        private readonly HashSet<Characteristic> m_SubscribedCharacteristics = new();
        private int? m_EventBlockHashCode;

        public HapClient(AccessoryServer server, TcpClient tcpClient, ILogger<HapClient> logger)
        {
            m_AccessoryServer = server;
            m_TcpClient = tcpClient;
            m_Logger = logger;

            m_Socket = tcpClient.Client;
            m_TcpStream = tcpClient.GetStream();

            m_Aead = new(logger);

            m_RemoteIp = m_Socket.RemoteEndPoint?.ToString() ?? "unknown";
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
                m_Logger.LogTrace("TCP Rx {length}", requestLength);

                if (requestLength == 0)
                {
                    break;
                }

                if (requestLength == m_ReadBuffer.Length)
                {
                    // todo handle - Array.Resize
                    throw new NotImplementedException("Read buffer overflow");
                }

                try
                {
                    int responseLength = ProcessRequest(requestLength);
                    if (responseLength == 0)
                    {
                        throw new Exception("Response length 0");
                    }

                    m_Logger.LogTrace("TCP Tx {length}", responseLength);
                    await m_TcpStream.WriteAsync(m_WriteBuffer.AsMemory(0, responseLength), CancellationToken.None);
                }
                catch (Exception ex)
                {
                    m_Logger.LogError(ex, "Failed to process request");
                }
            }

            m_Logger.LogWarning("Closing HAP client {remote}", m_Socket.RemoteEndPoint);

            foreach (var characteristic in m_SubscribedCharacteristics)
            {
                Unsubscribe(characteristic);
            }

            m_TcpClient.Close();
            m_AccessoryServer.RemoveClientReceiver(this);
        }

        private int ProcessRequest(int rxLength)
        {
            rxLength = m_Aead.Decrypt(m_ReadBuffer.AsSpan(0, rxLength), m_ReadBuffer.AsSpan());

            m_Logger.LogDebug("REQ:\n{data}", Encoding.UTF8.GetString(m_ReadBuffer.AsSpan(0, rxLength)));

            // todo cleanup
            var plain = Encoding.UTF8.GetString(m_ReadBuffer.AsSpan(0, rxLength));
            var plainHeaders = plain.Split(Environment.NewLine);
            var where = plainHeaders[0].Split(' ');

            var method = where[0];
            var path = where[1];
            //var version = where[2];

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

            //var host = string.Empty;
            //var contentType = string.Empty;
            var contentLength = 0;

            for (int i = 1; i < plainHeaders.Length; i++)
            {
                var split = plainHeaders[i].Split(": ", 2);
                if (split.Length == 2)
                {
                    //if (split[0] == "Host") host = split[1];
                    //if (split[0] == "Content-Type") contentType = split[1];
                    if (split[0] == "Content-Length") contentLength = int.Parse(split[1]);
                }
            }

            Span<byte> rx = Span<byte>.Empty;
            if (contentLength > 0)
            {
                rx = m_ReadBuffer.AsSpan(rxLength - contentLength, contentLength);
            }

            var tx = m_WriteBuffer.AsSpan();

            m_Logger.LogInformation("{remote} -> {method} {path} {query}", m_RemoteIp, method, path, query);

            var txLength = (method, path) switch
            {
                ("POST", "/pair-setup") => PairSetup(rx, tx),
                ("POST", "/pair-verify") => PairVerify(rx, tx),
                ("POST", "/pairings") => Pairings(rx, tx),
                ("GET", "/accessories") => GetAccessories(tx),
                ("GET", "/characteristics") => GetCharacteristics(tx, query),
                ("PUT", "/characteristics") => PutCharacteristics(rx, tx),
                _ => throw new NotImplementedException($"Method {method}, {path}, {query}"),
            };

            m_Logger.LogDebug("RES:\n{data}", Encoding.UTF8.GetString(m_WriteBuffer.AsSpan(0, txLength)));

            txLength = m_Aead.Encrypt(m_WriteBuffer.AsSpan(0, txLength), m_WriteBuffer.AsSpan());


            return txLength;
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
            /// doc says if its already paired but if more pairings is supported then continute
            if (m_AccessoryServer.HasReachedMaximumPairings())
            {
                return WriteError(tx, TlvError.Unavailable, 2);
            }

            /// 5.6.2 - 2
            var maxTries = false;
            if (maxTries)
            {
                return WriteError(tx, TlvError.MaxTries, 2);
            }

            /// 5.6.2 - 3
            var busy = false;
            if (busy)
            {
                return WriteError(tx, TlvError.Busy, 2);
            }

            /// 5.6.2 - 4,5,6,7
            Span<byte> salt = stackalloc byte[16];
            Random.Shared.NextBytes(salt);

            m_SrpServer.SetSalt(salt);
            m_SrpServer.SetUsernameAndPassword("Pair-Setup", m_AccessoryServer.GetPinCode());

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

            Span<byte> accessoryProof = stackalloc byte[64];
            if (!m_SrpServer.TryRespond(passwordProof, accessoryProof))
            {
                return WriteError(tx, TlvError.Authentication, 4);
            }

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
            bool decrypted = Crypto.DecryptEncryptedData(
                decryptedData, encryptedDataWithTag, sharedSecret,
                "Pair-Setup-Encrypt-Salt", "Pair-Setup-Encrypt-Info", "PS-Msg05"
            );
            if (!decrypted)
            {
                return WriteError(tx, TlvError.Authentication, 6);
            }

            /// 5.6.6.1 - 3
            Span<byte> iosDeviceX = stackalloc byte[32];
            Crypto.HkdfSha512DeriveKey(iosDeviceX, sharedSecret, "Pair-Setup-Controller-Sign-Salt", "Pair-Setup-Controller-Sign-Info");

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
                return WriteError(tx, TlvError.Authentication, 6);
            }

            /// 5.6.6.1 - 6
            var id = Utils.ReadUtf8Identifier(iosDevicePairingId);
            var pairedClient = new PairedClient()
            {
                Id = id,
                ClientLtPk = iosDeviceLtPk.ToArray(),
                ClientPermissions = 1
            };
            if (!m_AccessoryServer.TryAddPairedClient(pairedClient))
            {
                return WriteError(tx, TlvError.MaxPeers, 6);
            }

            m_Logger.LogInformation("Pairing added {id}", id);

            /// M6 Response Generation

            /// 5.6.6.2 - 1
            var accessoryLtSk = m_AccessoryServer.GetLtSk();
            var accessoryLtPk = m_AccessoryServer.GetLtPk();

            /// 5.6.6.2 - 2
            Span<byte> accessoryX = stackalloc byte[32];
            Crypto.HkdfSha512DeriveKey(accessoryX, sharedSecret, "Pair-Setup-Accessory-Sign-Salt", "Pair-Setup-Accessory-Sign-Info");

            /// 5.6.6.2 - 3
            Span<byte> accessoryPairingId = stackalloc byte[17];
            Utils.WriteUtf8Bytes(accessoryPairingId, m_AccessoryServer.GetMacAddress());

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
            Crypto.EncryptData(
                encryptedDataWithTag_tx, subTlv, sharedSecret,
                "Pair-Setup-Encrypt-Salt", "Pair-Setup-Encrypt-Info", "PS-Msg06"
            );

            /// 5.6.6.2 - 7
            Span<byte> content = stackalloc byte[140];
            var contentPosition = 0;
            contentPosition += TlvWriter.WriteTlv(content[contentPosition..], TlvType.State, 6);
            contentPosition += TlvWriter.WriteTlv(content[contentPosition..], TlvType.EncryptedData, encryptedDataWithTag_tx);

            m_Logger.LogInformation("Pairing setup complete {id}", id);

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
            var accessoryCurve = X25519KeyAgreement.GenerateKeyPair(); // todo 32, 32
            accessoryCurve.PublicKey.AsSpan().CopyTo(m_AccessoryCurvePk);

            /// 5.7.2 - 2
            Span<byte> iosDeviceCurvePK = stackalloc byte[32];
            TlvReader.ReadValue(TlvType.PublicKey, rx, iosDeviceCurvePK);

            var sharedSecret = X25519KeyAgreement.Agreement(accessoryCurve.PrivateKey, iosDeviceCurvePK.ToArray()); // todo 32
            sharedSecret.AsSpan().CopyTo(m_SharedSecret);
            iosDeviceCurvePK.CopyTo(m_IosDeviceCurvePk);

            /// 5.7.2 - 3
            Span<byte> accessoryPairingId = stackalloc byte[17];
            Utils.WriteUtf8Bytes(accessoryPairingId, m_AccessoryServer.GetMacAddress());

            Span<byte> accessoryInfo = stackalloc byte[81];
            accessoryCurve.PublicKey.CopyTo(accessoryInfo[0..]);
            accessoryPairingId.CopyTo(accessoryInfo[32..]);
            iosDeviceCurvePK.CopyTo(accessoryInfo[49..]);

            /// 5.7.2 - 4
            var accessorySignature = Signer.Sign(accessoryInfo, m_AccessoryServer.GetLtSk(), m_AccessoryServer.GetLtPk()); // todo 64

            /// 5.7.2 - 5
            Span<byte> subTlv = stackalloc byte[85];
            var subTlvPosition = 0;
            subTlvPosition += TlvWriter.WriteTlv(subTlv[subTlvPosition..], TlvType.Identifier, accessoryPairingId);
            subTlvPosition += TlvWriter.WriteTlv(subTlv[subTlvPosition..], TlvType.Signature, accessorySignature);

            /// 5.7.2 - 6, 7
            Span<byte> encryptedDataWithTag = stackalloc byte[101];
            Crypto.EncryptData(
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
            bool decrypted = Crypto.DecryptEncryptedData(
                decryptedData, encryptedDataWithTag, m_SharedSecret,
                "Pair-Verify-Encrypt-Salt", "Pair-Verify-Encrypt-Info", "PV-Msg03"
            );

            if (!decrypted)
            {
                m_Logger.LogError("Failed to decrypt data");
                return WriteError(tx, TlvError.Authentication, 4);
            }

            /// 5.7.4 - 3
            Span<byte> iosDevicePairingId = stackalloc byte[36];
            TlvReader.ReadValue(TlvType.Identifier, decryptedData, iosDevicePairingId);

            var iosDevicePairingGuid = Utils.ReadUtf8Identifier(iosDevicePairingId);

            var pairedClient = m_AccessoryServer.GetPairedClient(iosDevicePairingGuid);
            if (pairedClient is null)
            {
                m_Logger.LogError("Paired client {ClientGuid} was not found", iosDevicePairingGuid);
                return WriteError(tx, TlvError.Authentication, 4);
            }

            /// 5.7.4 - 4
            Span<byte> iosDeviceSignature = stackalloc byte[64];
            TlvReader.ReadValue(TlvType.Signature, decryptedData, iosDeviceSignature);

            Span<byte> iosDeviceInfo = stackalloc byte[100];
            m_IosDeviceCurvePk.CopyTo(iosDeviceInfo[0..]);
            iosDevicePairingId.CopyTo(iosDeviceInfo[32..]);
            m_AccessoryCurvePk.CopyTo(iosDeviceInfo[68..]);

            bool valid = Signer.Validate(iosDeviceSignature, iosDeviceInfo, pairedClient.ClientLtPk);
            if (!valid)
            {
                m_Logger.LogError("Failed to validate signature");
                return WriteError(tx, TlvError.Authentication, 4);
            }

            /// 5.7.4 - 5
            m_Aead.Enable(m_SharedSecret);
            m_PairedClient = pairedClient;

            m_Logger.LogInformation("Pairing verified {id}", iosDevicePairingGuid);

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
            m_Logger.LogInformation("Add pairing");

            /// 5.10.2 - 1, 2
            if (m_PairedClient is null || m_PairedClient.ClientPermissions != 1)
            {
                return WriteError(tx, TlvError.Authentication, 2);
            }

            /// 5.10.2 - 3, 4
            Span<byte> additionalControllerPairingIdentifier = stackalloc byte[36];
            TlvReader.ReadValue(TlvType.Identifier, rx, additionalControllerPairingIdentifier);

            Span<byte> additionalControllerLtPk = stackalloc byte[32];
            TlvReader.ReadValue(TlvType.PublicKey, rx, additionalControllerLtPk);

            var additionalControllerPermissions = TlvReader.ReadValue(TlvType.Permissions, rx);
            if (additionalControllerPermissions is null) throw new MissingFieldException("Permissions");

            var id = Utils.ReadUtf8Identifier(additionalControllerPairingIdentifier);
            var pairedClient = m_AccessoryServer.GetPairedClient(id);

            if (pairedClient is not null)
            {
                if (pairedClient.ClientLtPk.AsSpan().SequenceEqual(additionalControllerLtPk))
                {
                    pairedClient.ClientPermissions = additionalControllerPermissions.Value;
                    m_Logger.LogInformation("Updated {id}", id);
                }
                else
                {
                    return WriteError(tx, TlvError.Unknown, 2);
                }
            }
            else
            {
                pairedClient = new PairedClient()
                {
                    Id = id,
                    ClientLtPk = additionalControllerLtPk.ToArray(),
                    ClientPermissions = additionalControllerPermissions.Value
                };
                if (!m_AccessoryServer.TryAddPairedClient(pairedClient))
                {
                    return WriteError(tx, TlvError.MaxPeers, 2);
                }

                m_Logger.LogInformation("Added {id}", id);
            }

            /// 5.10.2 - 5,6
            return WritePairingState(tx, 2);
        }

        private int RemovePairing(ReadOnlySpan<byte> rx, Span<byte> tx)
        {
            m_Logger.LogInformation("Remove pairing");

            /// 5.11.2 - 1, 2
            if (m_PairedClient is null || m_PairedClient.ClientPermissions != 1)
            {
                return WriteError(tx, TlvError.Authentication, 2);
            }

            /// 5.11.2 - 3
            Span<byte> removedControllerPairingIdentifier = stackalloc byte[36];
            TlvReader.ReadValue(TlvType.Identifier, rx, removedControllerPairingIdentifier);
            var id = Utils.ReadUtf8Identifier(removedControllerPairingIdentifier);

            m_AccessoryServer.RemovePairedClient(id);
            m_Logger.LogInformation("Removed {id}", id);

            /// 5.11.2 - 6, 7
            if (m_PairedClient.Id == id)
            {
                m_StoppingToken.Cancel();
            }

            /// 5.11.2 - 4, 5
            return WritePairingState(tx, 2);
        }

        private int ListPairings(ReadOnlySpan<byte> rx, Span<byte> tx)
        {
            m_Logger.LogInformation("List pairings");

            /// 5.12.2 - 1, 2
            if (m_PairedClient is null || m_PairedClient.ClientPermissions != 1)
            {
                return WriteError(tx, TlvError.Authentication, 2);
            }

            /// 5.12.2 - 3
            var pairedClients = m_AccessoryServer.GetPairedClients();
            Span<byte> content = stackalloc byte[3 + ((75 + 2) * pairedClients.Length)];

            var contentPosition = 0;
            contentPosition += TlvWriter.WriteTlv(content[contentPosition..], TlvType.State, 2);

            Span<byte> additionalIdentifier = stackalloc byte[36];

            foreach (var pairedClient in pairedClients)
            {
                Utils.WriteUtf8Identifier(pairedClient.Id, additionalIdentifier);

                contentPosition += TlvWriter.WriteTlv(content[contentPosition..], TlvType.Identifier, additionalIdentifier);
                contentPosition += TlvWriter.WriteTlv(content[contentPosition..], TlvType.PublicKey, pairedClient.ClientLtPk);
                contentPosition += TlvWriter.WriteTlv(content[contentPosition..], TlvType.Permissions, pairedClient.ClientPermissions);

                content[contentPosition++] = (byte)TlvType.Separator;
                content[contentPosition++] = 0x00;
            }

            /// 5.12.2 - 4
            return WriteHapContent(tx, content[..^2]);
        }

        private int GetAccessories(Span<byte> tx)
        {
            var json = JsonSerializer.Serialize(m_AccessoryServer, Utils.HapJsonOptions);
            var jsonBytes = Encoding.UTF8.GetBytes(json);
            return WriteHapContent(tx, jsonBytes);
        }

        private int PutCharacteristics(ReadOnlySpan<byte> rx, Span<byte> tx)
        {
            /// 6.7.2
            var request = JsonSerializer.Deserialize<CharacteristicWriteRequest>(rx, Utils.HapJsonOptions);

            foreach (var characteristicWrite in request.Characteristics)
            {
                var characteristic = m_AccessoryServer.GetCharacteristic(characteristicWrite.Aid, characteristicWrite.Iid);
                if (characteristic is null)
                {
                    // todo handle error
                    throw new NotImplementedException("PutCharacteristics characteristic not found");
                }

                if (characteristicWrite.Ev is not null)
                {
                    if (characteristicWrite.Ev.Value)
                    {
                        Subscribe(characteristic);
                    }
                    else
                    {
                        Unsubscribe(characteristic);
                    }
                    continue;
                }

                if (characteristicWrite.Value is not null)
                {
                    m_EventBlockHashCode = characteristic.GetHashCode();
                    characteristic.ValueFromObject(characteristicWrite.Value);
                    m_EventBlockHashCode = null;
                }
            }

            return HttpWriter.WriteNoContent(tx);
        }

        private void Unsubscribe(Characteristic characteristic)
        {
            characteristic.Unsubscribe(OnSubscriptionValueChange);
            m_SubscribedCharacteristics.Remove(characteristic);
            m_Logger.LogInformation("Subscription removed for {aid}.{iid}", characteristic.Aid, characteristic.Iid);
        }

        private void Subscribe(Characteristic characteristic)
        {
            characteristic.Subscribe(OnSubscriptionValueChange);
            m_SubscribedCharacteristics.Add(characteristic);
            m_Logger.LogInformation("Subscription added for {aid}.{iid}", characteristic.Aid, characteristic.Iid);
        }

        private void OnSubscriptionValueChange(Characteristic sender, object newValue)
        {
            if (m_EventBlockHashCode == sender.GetHashCode())
            {
                m_Logger.LogDebug("Event skipped for {aid}.{iid}", sender.Aid, sender.Iid);
                return;
            }

            if (!m_TcpStream.CanWrite)
            {
                m_Logger.LogError("Cannot write event for {aid}.{iid}", sender.Aid, sender.Iid);
                return;
            }

            var response = new CharacteristicReadResponse()
            {
                Characteristics = new CharacteristicRead[]
                {
                    new()
                    {
                        Aid = sender.Aid,
                        Iid = sender.Iid,
                        Value = newValue,
                    }
                }
            };

            var json = JsonSerializer.Serialize(response, Utils.HapJsonOptions);
            var jsonbytes = Encoding.UTF8.GetBytes(json);

            var buffer = m_EventBuffer.AsSpan();

            var length = 0;
            length += HttpWriter.WriteHapEvent(buffer[length..]);
            length += HttpWriter.WriteContent(buffer[length..], jsonbytes);

            if (m_Logger.IsEnabled(LogLevel.Debug))
            {
                m_Logger.LogDebug("{remote} <- EVENT {aid}.{iid} {newValue}\n{data}", m_RemoteIp, sender.Aid, sender.Iid, newValue, Encoding.UTF8.GetString(buffer[..length]));
            }
            else
            {
                m_Logger.LogInformation("{remote} <- EVENT {aid}.{iid} {newValue}", m_RemoteIp, sender.Aid, sender.Iid, newValue);
            }

            length = m_Aead.Encrypt(buffer[..length], buffer);

            m_TcpStream.Write(buffer[..length]);
            m_Logger.LogTrace("TCP Tx {length} (EVENT)", length);
        }

        private int GetCharacteristics(Span<byte> tx, string query)
        {
            /// 6.7.4.2
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

                    var characteristic = m_AccessoryServer.GetCharacteristic(aid, iid);
                    if (characteristic is null)
                    {
                        // todo handle error
                        throw new NotImplementedException("GetCharacteristics characteristic not found");

                        //characteristics[i] = new CharacteristicRead()
                        //{
                        //    Aid = aid,
                        //    Iid = iid,
                        //    Status = HapStatusCode.ResourceDoesNotExist.GetHashCode(),
                        //};
                    }
                    else
                    {
                        characteristics[i] = new CharacteristicRead()
                        {
                            Aid = aid,
                            Iid = iid,
                            Value = characteristic.ValueToObject(),
                        };
                    }
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

    }
}
