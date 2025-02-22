namespace HomeKit.Characteristics.Abstract
{
    public abstract class StringCharacteristic : ACharacteristic
    {
        public override string Format => "string";
        public string Value { get; set; } = string.Empty;
        //object ACharacteristic.Value { get => Value; set => SetValue(value); }

        public delegate void ValueChange(Characteristic sender, string newValue);
        public event ValueChange? OnValueChange;

        //private void SetValue(object value)
        //{
        //    Value = value.ToString() ?? string.Empty;
        //}
    }
}
