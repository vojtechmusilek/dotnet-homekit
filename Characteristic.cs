using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using HomeKit.Resources;

namespace HomeKit
{
    /// 6.3.3
    public class Characteristic
    {
        private static readonly Dictionary<CharacteristicType, CharacteristicDef> m_Characteristics = new();

        static Characteristic()
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

        private readonly CharacteristicDef m_Def;

        public string Type { get; }
        public int Iid { get; set; }
        public object? Value { get; set; }
        public List<string> Perms => m_Def.Permissions;
        public string Format => m_Def.Format;

        public Characteristic(CharacteristicType type)
        {
            m_Def = m_Characteristics[type];

            Type = Utils.GetHapType(m_Def.Uuid);

            if (m_Def.Format == "string")
            {
                Value = string.Empty;
            }

            //if (m_Def.Format == "bool")
            //{
            //    Value = false;
            //}

            //if (m_Def.ValidValues is not null)
            //{
            //    Value = m_Def.ValidValues.First().Value;
            //}

            //if (m_Def.MinValue is not null)
            //{
            //    Value = m_Def.MinValue.Value;
            //}

            //if (Value is null)
            //{
            //    throw new NotImplementedException();
            //}
        }

        public bool IsType(CharacteristicType type)
        {
            return m_Def.Type == type;
        }
    }
}