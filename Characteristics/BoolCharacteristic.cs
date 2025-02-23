namespace HomeKit.Characteristics
{
    public abstract class BoolCharacteristic(string type, string[] perms) : Characteristic<bool>(type, perms, "bool")
    {
        
    }
}
