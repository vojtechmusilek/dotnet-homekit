using HomeKit.Resources;

namespace HomeKit
{
    /// 6.3.3
    public class Characteristic : ICharacteristic
    {
        private readonly CharacteristicDef m_Def;

        private object? m_Value;

        public delegate void ValueChange(Characteristic sender, object newValue);
        public event ValueChange? OnValueChange;

        public string Type { get; }
        public int Iid { get; set; }
        public string Format => m_Def.Format;
        public string[] Perms => m_Def.Permissions;
        public object? Value { get => m_Value; set => ValueSetter(value); }

        public Characteristic(CharacteristicType type)
        {
            m_Def = CharacteristicDefs.Get(type);

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