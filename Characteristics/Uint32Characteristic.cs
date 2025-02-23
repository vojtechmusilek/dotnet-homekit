namespace HomeKit.Characteristics
{
    public abstract class Uint32Characteristic(string type, string[] perms) : Characteristic<uint>(type, perms, "uint32")
    {
    }
}
