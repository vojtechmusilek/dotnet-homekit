using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using Ed25519;

namespace HomeKit
{
    public class ServerState(string filePath)
    {
        public required byte[] ServerLtSk { get; set; }
        public required byte[] ServerLtPk { get; set; }
        public List<PairedClient> PairedClients { get; } = new();

        private string m_FilePath = filePath;

        public static ServerState FromPath(string path, string mac)
        {
            var directoryPath = Path.GetDirectoryName(path);
            var filePath = Path.Join(directoryPath, $"server_state_{mac.Replace(':', '_')}.json");

            if (!File.Exists(filePath))
            {
                var secretKey = Signer.GeneratePrivateKey();
                var publicKey = secretKey.ExtractPublicKey();

                return new ServerState(filePath)
                {
                    ServerLtSk = secretKey.ToArray(),
                    ServerLtPk = publicKey.ToArray()
                };
            }

            var state = JsonSerializer.Deserialize<ServerState>(filePath);

            state!.m_FilePath = filePath;

            return state!;
        }

        public void Save()
        {
            // todo
            System.Console.WriteLine(m_FilePath);
        }
    }

    public class PairedClient
    {
        public required Guid Id { get; set; }
        public required byte[] ClientLtPk { get; set; }
        public required byte ClientPermissions { get; set; }
    }
}
