using System.Collections.Generic;
using System.Text.Json.Serialization;
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

        [JsonIgnore] public AccessoryServer Server { get; }
        [JsonIgnore] public string Name { get; }
        [JsonIgnore] public Category Category { get; }

        public Accessory(string name, AccessoryServer server, Category? category, ILoggerFactory? loggerFactory)
        {
            Name = name;

            Category = category ?? Category.Other;

            loggerFactory ??= new NullLoggerFactory();
            m_LoggerFactory = loggerFactory;
            m_Logger = loggerFactory.CreateLogger($"Accessory<{name}>");

            // todo notify server about this new accy
            Server = server;
            Server.Accessories.Add(this);

            AddAccessoryInformationService();
        }

        public Service AddService(ServiceType type)
        {
            var service = new Service(type);
            Services.Add(service);
            return service;
        }

        private void AddAccessoryInformationService()
        {
            var service = new Service(ServiceType.AccessoryInformation);
            service.GetCharacteristic(CharacteristicType.Name)!.Value = Name;
            service.GetCharacteristic(CharacteristicType.SerialNumber)!.Value = "SN-" + Name;

            Services.Add(service);
        }

    }
}
