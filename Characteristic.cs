using System.Text.Json.Serialization;

namespace HomeKit
{
    public abstract class Characteristic<T>(string type, string[] perms, string format) : Characteristic(type, perms, format)
    {
        private T? m_Value;

        public T? Value { get => m_Value; set => SetValue(value); }

        public delegate void ValueChange(Characteristic<T> sender, T newValue);
        public event ValueChange? Changed;

        protected virtual void SetValue(T? value)
        {
            m_Value = value;

            if (m_Value is not null)
            {
                Changed?.Invoke(this, m_Value);
            }
        }
    }

    public abstract class Characteristic(string type, string[] perms, string format)
    {
        [JsonIgnore]
        public int Aid { get; set; }
        public int Iid { get; set; }
        public string Type { get; } = type;
        public string[] Perms { get; } = perms;
        public string Format { get; } = format;
    }
}
