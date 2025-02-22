using System;
using HomeKit.Characteristics.Abstract;
using HomeKit.Resources;

namespace HomeKit
{
    /// 6.3.3
    public class Characteristic // : ACharacteristic
    {
        private readonly CharacteristicDef m_Def;

        private object? m_Value;

        public delegate void ValueChange(Characteristic sender, object newValue);
        public event ValueChange? OnValueChange;

        public int Aid { get; set; }
        public int Iid { get; set; }
        public string Type { get; }
        public string Format => m_Def.Format;
        public string[] Perms => m_Def.Permissions;
        // todo nullable
        public object? Value { get => m_Value; set => ValueSetter(value); }

        public Characteristic(CharacteristicType type)
        {
            m_Def = CharacteristicDefs.Get(type);

            Type = Utils.GetHapType(m_Def.Uuid);
        }

        private void ValueSetter(object? newValue)
        {
            /// because ios device can send 1/0 instead of bool
            if (m_Def.Format == "bool")
            {
                newValue = Convert.ToBoolean(newValue);
            }

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