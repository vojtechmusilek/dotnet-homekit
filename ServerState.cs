using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using Ed25519;

namespace HomeKit
{
    public class ServerState
    {
        public required byte[] ServerLtSk { get; init; }
        public required byte[] ServerLtPk { get; init; }
        public required List<PairedClient> PairedClients { get; init; }

        public static ServerState FromPath(string path, string mac)
        {
            var directoryPath = Path.GetDirectoryName(path);
            var filePath = Path.Join(directoryPath, $"server_state_{mac.Replace(':', '_')}.json");

            if (!File.Exists(filePath))
            {
                var secretKey = Signer.GeneratePrivateKey();
                var publicKey = secretKey.ExtractPublicKey();

                return new ServerState()
                {
                    ServerLtSk = secretKey.ToArray(),
                    ServerLtPk = publicKey.ToArray(),
                    PairedClients = new()
                };
            }

            using var fileStream = File.OpenRead(filePath);
            var state = JsonSerializer.Deserialize<ServerState>(fileStream);

            return state!;
        }

        public void Save(string path, string mac)
        {
            var directoryPath = Path.GetDirectoryName(path);
            var filePath = Path.Join(directoryPath, $"server_state_{mac.Replace(':', '_')}.json");

            using var fileStream = File.OpenWrite(filePath);

            JsonSerializer.Serialize(fileStream, this, new JsonSerializerOptions()
            {
                WriteIndented = true
            });
        }
    }

    public class PairedClient
    {
        public required Guid Id { get; init; }
        public required byte[] ClientLtPk { get; init; }
        public required byte ClientPermissions { get; set; }
    }
}
