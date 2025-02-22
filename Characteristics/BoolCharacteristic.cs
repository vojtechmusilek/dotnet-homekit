namespace HomeKit.Characteristics
{
    public abstract class BoolCharacteristic(string type, string[] perms) : Characteristic(type, perms, "bool")
    {
        public bool Value { get; set; }

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
