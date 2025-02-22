namespace HomeKit.Characteristics
{
    public abstract class IntCharacteristic(string type, string[] perms) : Characteristic(type, perms, "int")
    {
        public int Value { get; set; }
    }
}
