namespace HomeKit.Characteristics.Abstract
{
    public abstract class BoolCharacteristic : ACharacteristic
    {
        public override string Format => "bool";
        public bool Value { get; set; }
        //object ACharacteristic.Value { get => Value; set => SetValue(value); }

        public delegate void ValueChange(Characteristic sender, bool newValue);
        public event ValueChange? OnValueChange;

        public void SetValue(object value)
        {
            Value = value switch
            {
                bool val => val,
                double val => val == 1,
                _ => Value,
            };
        }
    }
}
