namespace HomeKit.Characteristics
{
    public abstract class StringCharacteristic(string type, string[] perms) : Characteristic<string>(type, perms, "string")
    {
    }
}
