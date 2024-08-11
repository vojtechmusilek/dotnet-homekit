namespace HomeKit
{
    public abstract class StringCharacteristic : ICharacteristic
    {
        public int Aid { get; }
        public int Iid { get; }
        public abstract string Type { get; }
        public abstract string[] Perms { get; }
        public string Format => "string";
        public string Value { get; set; } = string.Empty;
        object ICharacteristic.Value { get => Value; set => SetValue(value); }

        private void SetValue(object value)
        {
            Value = value.ToString() ?? string.Empty;
        }
    }
}
