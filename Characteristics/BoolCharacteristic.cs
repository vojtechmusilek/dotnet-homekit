using System;

namespace HomeKit.Characteristics
{
    public abstract class BoolCharacteristic(string type, string[] perms) : Characteristic<bool>(type, perms, "bool")
    {
        internal override void ValueFromObject(object? value)
        {
            Value = Convert.ToBoolean(value);
        }
    }
}
