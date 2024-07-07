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
            var deserialized = JsonSerializer.Deserialize<ServiceDef[]>(stream, Utils.HapDefJsonOptions)!;
            foreach (var item in deserialized)
            {
                m_Services.Add(item.Type, item);
            }
        }

        private readonly ServiceDef m_Def;

        public int Iid { get; set; }
        public string Type { get; }
        public List<Characteristic> Characteristics { get; }

        public Service(ServiceType serviceType)
        {
            m_Def = m_Services[serviceType];

            Type = Utils.GetHapType(m_Def.Uuid);
            Characteristics = new();

            foreach (var type in m_Def.RequiredCharacteristics)
            {
                Characteristics.Add(new Characteristic(type));
            }
        }

        public Characteristic? AddCharacteristic(CharacteristicType type)
        {
            if (!m_Def.OptionalCharacteristics.Contains(type))
            {
                return null;
            }

            var characteristic = new Characteristic(type);
            Characteristics.Add(characteristic);
            return characteristic;
        }

        public Characteristic? GetCharacteristic(CharacteristicType type)
        {
            return Characteristics.Find(x => x.IsType(type));
        }
    }
}
