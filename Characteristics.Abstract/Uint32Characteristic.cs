namespace HomeKit.Characteristics.Abstract
{
    public abstract class Uint32Characteristic : ACharacteristic
    {
        public override string Format => "uint32";

        public uint Value { get; set; }
    }
}
