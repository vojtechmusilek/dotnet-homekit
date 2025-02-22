namespace HomeKit.Characteristics
{
    public abstract class Uint8Characteristic(string type, string[] perms) : Characteristic(type, perms, "uint8")
    {
        public byte Value { get; set; }
    }
}
