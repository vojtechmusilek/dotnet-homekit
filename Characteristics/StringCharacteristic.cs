using System;

namespace HomeKit.Characteristics
{
    public abstract class StringCharacteristic(string type, string[] perms) : Characteristic<string>(type, perms, "string")
    {
        internal override void ValueFromObject(object? value)
        {
            Value = Convert.ToString(value);
        }
    }
}
