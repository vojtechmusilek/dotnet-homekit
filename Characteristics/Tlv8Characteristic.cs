using System;

namespace HomeKit.Characteristics
{
    // todo
    public abstract class Tlv8Characteristic(string type, string[] perms) : Characteristic<string>(type, perms, "tlv8")
    {
        internal override void ValueFromObject(object? value)
        {
            throw new NotImplementedException();
        }
    }
}
