using System;
using System.Collections.Generic;

namespace HomeKit.Characteristics
{
    public abstract class FloatCharacteristic(string type, string[] perms) : Characteristic<float>(type, perms, "float")
    {
        // todo fill
        public virtual Dictionary<string, int>? ValidValues { get; }
        public virtual float? MaxValue { get; }
        public virtual float? MinValue { get; }
        public virtual float? MinStep { get; }

        protected internal override void SetValue(float value)
        {
            if (ValidValues is not null)
            {
                if (!ValidValues.ContainsValue((int)value))
                {
                    return;
                }
            }

            if (MaxValue is not null)
            {
                value = Math.Min(value, MaxValue.Value);
            }

            if (MinValue is not null)
            {
                value = Math.Max(value, MinValue.Value);
            }

            if (MinStep is not null)
            {
                value = (float)(Math.Round((double)value / (double)MinStep.Value) * (double)MinStep.Value);
            }

            base.SetValue(value);
        }

        internal override void ValueFromObject(object? value)
        {
            Value = Convert.ToSingle(value);
        }
    }
}
