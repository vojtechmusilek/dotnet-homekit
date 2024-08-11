namespace HomeKit
{
    public abstract class BoolCharacteristic : ICharacteristic
    {
        public int Aid { get; }
        public int Iid { get; }
        public abstract string Type { get; }
        public abstract string[] Perms { get; }
        public string Format => "bool";
        public bool Value { get; set; }
        object ICharacteristic.Value { get => Value; set => SetValue(value); }

        private void SetValue(object value)
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
