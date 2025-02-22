namespace HomeKit.Characteristics
{
    public abstract class Tlv8Characteristic(string type, string[] perms) : Characteristic(type, perms, "tlv8")
    {
        // todo
        public string Value { get; set; } = "";
    }
}
