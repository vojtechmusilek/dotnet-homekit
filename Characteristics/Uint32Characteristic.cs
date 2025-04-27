using System;

namespace HomeKit.Characteristics
{
    public abstract class Uint32Characteristic(string type, string[] perms) : Characteristic<uint>(type, perms, "uint32")
    {
        internal override void ValueFromObject(object? value)
        {
            Value = Convert.ToUInt32(value);
        }
    }
}
