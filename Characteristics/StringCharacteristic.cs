namespace HomeKit.Characteristics
{
    public abstract class StringCharacteristic(string type, string[] perms) : Characteristic(type, perms, "string")
    {
        public string Value { get; set; } = string.Empty;

        public delegate void ValueChange(Characteristic sender, string newValue);
        public event ValueChange? OnValueChange;
    }
}
