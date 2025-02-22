namespace HomeKit.Characteristics.Abstract
{
    public abstract class Tlv8Characteristic : ACharacteristic
    {
        public override string Format => "tlv8";

        // todo
        public string Value { get; set; } = "";
    }
}
