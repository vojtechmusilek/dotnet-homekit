using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.Json;

namespace HomeKit.Resources
{
    public class CharacteristicDefs
    {
        private static readonly Dictionary<CharacteristicType, CharacteristicDef> m_Characteristics = new();

        static CharacteristicDefs()
        {
            var assembly = Assembly.GetExecutingAssembly();
            var resources = assembly.GetManifestResourceNames();
            var resource = resources.First(x => x.Contains("Characteristics.json"));

            using var stream = assembly.GetManifestResourceStream(resource)!;
            var deserialized = JsonSerializer.Deserialize<CharacteristicDef[]>(stream, Utils.HapDefJsonOptions)!;
            foreach (var item in deserialized)
            {
                m_Characteristics.Add(item.Type, item);
            }
        }

        public static CharacteristicDef Get(CharacteristicType type)
        {
            return m_Characteristics[type];
        }
    }
}
