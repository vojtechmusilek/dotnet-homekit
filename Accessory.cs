using System;
using HomeKit.Resources;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using QRCoder;

namespace HomeKit
{
    internal class Accessory : IDisposable
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

            AddAccessoryInformationService();
        }

        public void Publish()
        {
            if (!Paired)
            {
                PrintSetupMessage();
            }

            m_Logger.LogTrace("MAC address: {mac}", MacAddress);

            // todo wip
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
            throw new NotImplementedException();
        }
    }
}
