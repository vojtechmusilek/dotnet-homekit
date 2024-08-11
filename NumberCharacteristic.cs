using System;
using System.Collections.Generic;

namespace HomeKit
{
    public abstract class NumberCharacteristic : ICharacteristic
    {
        private double m_Value;

        public int Aid { get; }
        public int Iid { get; }
        public abstract string Type { get; }
        public abstract string[] Perms { get; }
        public abstract string Format { get; }
        public double Value { get => m_Value; set => SetValue(value); }
        object ICharacteristic.Value { get => Value; set => Value = (double)value; }
        public virtual Dictionary<string, int>? ValidValues { get; }
        public virtual double? MaxValue { get; }
        public virtual double? MinValue { get; }
        public virtual double? MinStep { get; }

        private void SetValue(double value)
        {
            if (ValidValues is not null)
            {
                if (ValidValues.ContainsValue((int)value))
                {
                    m_Value = value;
                }

                return;
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
                value = Math.Round(value / MinStep.Value) * MinStep.Value;
            }

            m_Value = value;
        }
    }
}
