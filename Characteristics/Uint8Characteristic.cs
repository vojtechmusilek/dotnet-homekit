namespace HomeKit.Characteristics
{
    public abstract class Uint8Characteristic(string type, string[] perms) : Characteristic<byte>(type, perms, "uint8")
    {
    }
}
