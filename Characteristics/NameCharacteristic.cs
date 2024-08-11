namespace HomeKit.Characteristics
{
    public class NameCharacteristic : StringCharacteristic
    {
        public override string Type => "23";
        public override string[] Perms => ["pr"];
    }
}
