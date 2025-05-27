using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;

namespace HomeKit
{
    public delegate void ValueChangeCallback(Characteristic characteristic, object value);
    public delegate void ValueChangeCallback<T>(Characteristic<T> characteristic, T value);

    public abstract class Characteristic<T>(string type, string[] perms, string format) : Characteristic(type, perms, format)
    {
        private readonly HashSet<ValueChangeCallback> m_Subscriptions = new();
        private readonly HashSet<ValueChangeCallback<T>> m_SubscriptionsGeneric = new();

        private T? m_Value;
        public T? Value { get => m_Value; set => SetValue(value); }

        [Obsolete("Use ValueChangeCallback with Subscribe method")]
        public delegate void ValueChange(Characteristic<T> sender, T newValue);
        [Obsolete("Use .Subscribe(YourMethod) OR .Subscribe((sender, newValue) => { ... }) instead of .Changed += ...")]
        public event ValueChange? Changed;

        protected internal virtual void SetValue(T? value)
        {
            if (value is not null && value.Equals(m_Value))
            {
                return;
            }

            m_Value = value;

            if (m_Value is not null)
            {
                Changed?.Invoke(this, m_Value);

                foreach (var sub in m_Subscriptions)
                {
                    InvokeSafe(sub, m_Value);
                }

                foreach (var sub in m_SubscriptionsGeneric)
                {
                    InvokeSafe(sub, m_Value);
                }
            }
        }

        private void InvokeSafe(ValueChangeCallback sub, object value)
        {
            try
            {
                sub.Invoke(this, value);
            }
            catch (Exception ex)
            {
                if (Logger is null)
                {
                    Console.WriteLine($"Subscription invoke failed: {ex.Message}\n{ex.StackTrace}");
                }
                else
                {
                    Logger.LogError(ex, "Subscription invoke failed");
                }
            }
        }

        private void InvokeSafe(ValueChangeCallback<T> sub, T value)
        {
            try
            {
                sub.Invoke(this, value);
            }
            catch (Exception ex)
            {
                if (Logger is null)
                {
                    Console.WriteLine($"Subscription invoke failed: {ex.Message}\n{ex.StackTrace}");
                }
                else
                {
                    Logger.LogError(ex, "Subscription invoke failed");
                }
            }
        }

        internal override object? ValueToObject()
        {
            return m_Value;
        }

        public override void Subscribe(ValueChangeCallback callback)
        {
            m_Subscriptions.Add(callback);
        }

        public override void Unsubscribe(ValueChangeCallback callback)
        {
            m_Subscriptions.Remove(callback);
        }

        public void Subscribe(ValueChangeCallback<T> callback)
        {
            m_SubscriptionsGeneric.Add(callback);
        }

        public void Unsubscribe(ValueChangeCallback<T> callback)
        {
            m_SubscriptionsGeneric.Remove(callback);
        }
    }

    public abstract class Characteristic(string type, string[] perms, string format)
    {
        [JsonIgnore]
        internal int Aid { get; set; }
        [JsonInclude]
        internal int Iid { get; set; }
        public string Type { get; } = type;
        public string[] Perms { get; } = perms;
        public string Format { get; } = format;
        public ILogger? Logger { get; set; }

        internal abstract object? ValueToObject();
        internal abstract void ValueFromObject(object? value);
        public abstract void Subscribe(ValueChangeCallback callback);
        public abstract void Unsubscribe(ValueChangeCallback callback);
    }
}
