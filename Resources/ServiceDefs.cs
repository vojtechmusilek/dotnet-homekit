using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.Json;

namespace HomeKit.Resources
{
    public static class ServiceDefs
    {
        private static readonly Dictionary<ServiceType, ServiceDef> m_Services = new();

        static ServiceDefs()
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

        public static ServiceDef Get(ServiceType type)
        {
            return m_Services[type];
        }
    }
}
