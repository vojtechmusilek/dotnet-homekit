namespace HomeKit.Characteristics
{
    public abstract class Uint32Characteristic(string type, string[] perms) : Characteristic(type, perms, "uint32")
    {
        public uint Value { get; set; }
    }
}
