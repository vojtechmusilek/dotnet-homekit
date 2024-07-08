using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using HomeKit.Resources;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace HomeKit
{
    public class Accessory
    {
        private readonly ILoggerFactory m_LoggerFactory;
        private readonly ILogger m_Logger;

        private readonly string m_Name;
        private readonly Category m_Category;

        public int Aid { get; set; }
        public List<Service> Services { get; } = new();

        public Accessory(string name, Category? category, ILoggerFactory? loggerFactory)
        {
            m_Name = name;
            m_Category = category ?? Category.Other;

            loggerFactory ??= new NullLoggerFactory();
            m_LoggerFactory = loggerFactory;
            m_Logger = loggerFactory.CreateLogger($"Accessory<{name}>");

            AddAccessoryInformationService();
        }

        public Service AddService(ServiceType type)
        {
            var service = new Service(type);
            Services.Add(service);
            return service;
        }

        protected virtual AccessoryServer PrepareServer(AccessoryServerOptions options)
        {
            var server = new AccessoryServer(options);
            server.Accessories.Add(this);
            return server;
        }

        public async Task<AccessoryServer> PublishAsync(AccessoryServerOptions options, CancellationToken cancellationToken)
        {
            options.Name ??= m_Name;
            options.Category ??= m_Category;

            var server = PrepareServer(options);
            await server.StartAsync(cancellationToken);

            return server;
        }

        private void AddAccessoryInformationService()
        {
            var service = new Service(ServiceType.AccessoryInformation);
            service.GetCharacteristic(CharacteristicType.Name)!.Value = m_Name;
            service.GetCharacteristic(CharacteristicType.SerialNumber)!.Value = m_Name + " SerialNumber";
            service.GetCharacteristic(CharacteristicType.Manufacturer)!.Value = m_Name + " Manufacturer";
            service.GetCharacteristic(CharacteristicType.Model)!.Value = m_Name + " Model";
            service.GetCharacteristic(CharacteristicType.FirmwareRevision)!.Value = "1.0";
            //service.GetCharacteristic(CharacteristicType.Identify)!.Value = null;

            Services.Add(service);
        }

    }
}
