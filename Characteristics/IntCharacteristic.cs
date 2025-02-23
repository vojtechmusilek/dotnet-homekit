namespace HomeKit.Characteristics
{
    public abstract class IntCharacteristic(string type, string[] perms) : Characteristic<int>(type, perms, "int")
    {
    }
}
