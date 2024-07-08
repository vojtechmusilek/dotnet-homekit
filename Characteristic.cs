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

        private object? m_Value;

        public delegate void ValueChange(Characteristic sender, object newValue);
        public event ValueChange? OnValueChange;

        public string Type { get; }
        public int Iid { get; set; }
        public string Format => m_Def.Format;
        public List<string> Perms => m_Def.Permissions;
        public object? Value { get => m_Value; set => ValueSetter(value); }

        public Characteristic(CharacteristicType type)
        {
            m_Def = m_Characteristics[type];
            Type = Utils.GetHapType(m_Def.Uuid);
        }

        private void ValueSetter(object? newValue)
        {
            if (m_Value == newValue || newValue is null)
            {
                return;
            }

            m_Value = newValue;
            OnValueChange?.Invoke(this, newValue);
        }

        public bool IsType(CharacteristicType type)
        {
            return m_Def.Type == type;
        }
    }
}