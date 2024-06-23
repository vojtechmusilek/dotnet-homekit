using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using HomeKit.Mdns;
using HomeKit.Resources;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using QRCoder;

namespace HomeKit
{
    public class Accessory : IDisposable
    {
        private readonly ILoggerFactory m_LoggerFactory;
        private readonly ILogger m_Logger;

        public string Name { get; }
        public string PinCode { get; }
        public Category Category { get; }
        public string SetupId { get; }
        public string MacAddress { get; }
        public bool Paired { get; }

        /// <param name="pinCode">Format: 000-00-000</param>
        public Accessory(string name, string pinCode, Category category, ILoggerFactory? loggerFactory)
        {
            Name = name;
            PinCode = pinCode;
            Category = category;
            SetupId = Utils.GenerateSetupId();
            MacAddress = Utils.GenerateMacAddress();
            Paired = false;

            loggerFactory ??= new NullLoggerFactory();
            m_LoggerFactory = loggerFactory;
            m_Logger = loggerFactory.CreateLogger($"Accessory<{name}>");

            // todo
            //AddAccessoryInformationService();
        }

        public void Publish()
        {
            if (!Paired)
            {
                PrintSetupMessage();
            }

            m_Logger.LogTrace("MAC address: {mac}", MacAddress);

            foreach (var ni in Utils.GetMulticastNetworkInterfaces())
            {
                var adapterIndex = ni.GetIPProperties().GetIPv4Properties().Index;
                var client = new UdpClient();
                var socket = client.Client;

                socket.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.MulticastInterface, IPAddress.HostToNetworkOrder(adapterIndex));
                client.ExclusiveAddressUse = false;
                socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
                var localEp = new IPEndPoint(IPAddress.Any, 5353);
                socket.Bind(localEp);
                var multicastAddress = IPAddress.Parse("224.0.0.251");
                var multOpt = new MulticastOption(multicastAddress, adapterIndex);
                socket.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.AddMembership, multOpt);

                Task.Run(() => ClientTask(client));
            }
        }

        private async Task ClientTask(UdpClient client)
        {
            StringBuilder sb = new();

            while (client.Client.IsBound)
            {
                var res = await client.ReceiveAsync();

                if (res.RemoteEndPoint.Address.ToString() != "192.168.1.110")
                {
                    continue;
                }

                m_Logger.LogTrace("{data}", BitConverter.ToString(res.Buffer));


                sb.Clear();

                try
                {
                    var packet = PacketReader.ReadPacket(res.Buffer);
                    sb.AppendLine(packet.ToString());

                    foreach (var question in packet.Questions)
                    {
                        sb.AppendLine(question.ToString());
                    }
                }
                catch (Exception ex)
                {
                    sb.AppendLine(ex.Message);
                }

                m_Logger.LogInformation("UDP: {source} {len} bytes\n{packet}", res.RemoteEndPoint, res.Buffer.Length, sb.ToString());
            }

            client.Dispose();
        }

        private void AddAccessoryInformationService()
        {
            var service = new Service(ServiceType.AccessoryInformation);
            service.GetCharacteristic(CharacteristicType.Name)!.SetValue(Name);
            service.GetCharacteristic(CharacteristicType.SerialNumber)!.SetValue("SN-" + Name);

            AddService(service);
        }

        private void AddService(Service service)
        {
            throw new NotImplementedException();
        }

        // todo validate if works correctly
        private void PrintSetupMessage()
        {
            var uri = Utils.GenerateXhmUri(Category, PinCode, SetupId);

            m_Logger.LogTrace("Setup payload: {payload}", uri);
            m_Logger.LogInformation("Setup pincode: {pincode}", PinCode);

            var qrGenerator = new QRCodeGenerator();
            var qrCodeData = qrGenerator.CreateQrCode(uri, QRCodeGenerator.ECCLevel.Q);
            var qrCode = new AsciiQRCode(qrCodeData);
            var qrCodeAsAsciiArt = qrCode.GetGraphic(1);
            m_Logger.LogInformation("{qrcode}", qrCodeAsAsciiArt);

        }

        public void Dispose()
        {
            //throw new NotImplementedException();
        }

    }
}
