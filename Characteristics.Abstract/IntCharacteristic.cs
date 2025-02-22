namespace HomeKit.Characteristics.Abstract
{
    public abstract class IntCharacteristic : ACharacteristic
    {
        public override string Format => "int";

        public int Value { get; set; }
    }
}
