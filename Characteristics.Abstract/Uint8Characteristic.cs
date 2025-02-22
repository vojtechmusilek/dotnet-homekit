namespace HomeKit.Characteristics.Abstract
{
    public abstract class Uint8Characteristic : ACharacteristic
    {
        public override string Format => "uint8";

        public byte Value { get; set; }
    }
}
