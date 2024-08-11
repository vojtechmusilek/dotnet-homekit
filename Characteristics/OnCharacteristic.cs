namespace HomeKit.Characteristics
{
    public class OnCharacteristic : BoolCharacteristic
    {
        public override string Type => "25";
        public override string[] Perms => ["pr", "pw", "ev"];
    }
}
