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

        private readonly ServiceDef m_Def;

        public int Iid { get; }
        public string Type { get; }
        public List<Characteristic> Characteristics { get; }

        public Service(ServiceType type)
        {
            m_Def = m_Services[type];

            Iid = AccessoryServer.GenerateInstanceId();
            Type = m_Def.GetHapType();
            Characteristics = new();

            foreach (var item in m_Def.RequiredCharacteristics)
            {
                // todo create
            }
        }

        internal Characteristic? GetCharacteristic(CharacteristicType name)
        {
            throw new NotImplementedException();
        }
    }
}
