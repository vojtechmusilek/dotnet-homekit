using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using Ed25519;

namespace HomeKit
{
    public class ServerState
    {
        private static readonly JsonSerializerOptions m_JsonOptions = new()
        {
            WriteIndented = true
        };

        public byte[] ServerLtSk { get; init; }
        public byte[] ServerLtPk { get; init; }
        public List<PairedClient> PairedClients { get; init; }

        public ServerState()
        {
            var secretKey = Signer.GeneratePrivateKey();
            var publicKey = secretKey.ExtractPublicKey();
            ServerLtSk = secretKey.ToArray();
            ServerLtPk = publicKey.ToArray();
            PairedClients = new();
        }

        public static ServerState Load(string filePath)
        {
            if (File.Exists(filePath))
            {
                using var fileStream = File.OpenRead(filePath);
                return JsonSerializer.Deserialize<ServerState>(fileStream)!;
            }

            return new ServerState();
        }

        public void Save(string filePath)
        {
            using var fileStream = File.Create(filePath);
            JsonSerializer.Serialize(fileStream, this, m_JsonOptions);
        }
    }

    public class PairedClient
    {
        public required Guid Id { get; init; }
        public required byte[] ClientLtPk { get; init; }
        public required byte ClientPermissions { get; set; }
    }
}
