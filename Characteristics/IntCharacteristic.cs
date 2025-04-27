using System;

namespace HomeKit.Characteristics
{
    public abstract class IntCharacteristic(string type, string[] perms) : Characteristic<int>(type, perms, "int")
    {
        internal override void ValueFromObject(object? value)
        {
            Value = Convert.ToInt32(value);
        }
    }
}
