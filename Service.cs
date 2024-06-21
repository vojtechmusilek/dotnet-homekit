using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using HomeKit.Resources;

namespace HomeKit
{
    public class Service
    {
        private static readonly Dictionary<ServiceType, ServiceDef> m_Services = new();

        static Service()
        {
            var assembly = Assembly.GetExecutingAssembly();
            var resources = assembly.GetManifestResourceNames();
            var resource = resources.First(x => x.Contains("Services.json"));

            using var stream = assembly.GetManifestResourceStream(resource)!;
            var deserialized = JsonSerializer.Deserialize<ServiceDef[]>(stream)!;
            foreach (var item in deserialized)
            {
                m_Services.Add(item.Type, item);
            }
        }

        public ServiceDef Def { get; set; }

        public Service(ServiceType type)
        {
            Def = m_Services[type];
        }

        internal Characteristic? GetCharacteristic(CharacteristicType name)
        {
            throw new NotImplementedException();
        }
    }
}
