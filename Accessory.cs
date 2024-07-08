using System.Collections.Generic;
using System.Text.Json.Serialization;
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

        public int Aid { get; set; }
        public List<Service> Services { get; } = new();

        [JsonIgnore] public string Name { get; }
        [JsonIgnore] public Category Category { get; }

        public Accessory(string name, Category? category, ILoggerFactory? loggerFactory)
        {
            Name = name;

            Category = category ?? Category.Other;

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

        public async Task<AccessoryServer> PublishAsync(AccessoryServerOptions options, CancellationToken cancellationToken)
        {
            options.Name ??= Name;
            options.Category ??= Category;

            var server = new AccessoryServer(options);
            server.Accessories.Add(this);

            await server.StartAsync(cancellationToken);

            return server;
        }

        private void AddAccessoryInformationService()
        {
            var service = new Service(ServiceType.AccessoryInformation);
            service.GetCharacteristic(CharacteristicType.Name)!.Value = Name;
            service.GetCharacteristic(CharacteristicType.SerialNumber)!.Value = "SN-" + Name;
            //service.GetCharacteristic(CharacteristicType.Identify)!.Value = null;

            Services.Add(service);
        }

    }
}
