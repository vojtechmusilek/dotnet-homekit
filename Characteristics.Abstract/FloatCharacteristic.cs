﻿using System;
using System.Collections.Generic;

namespace HomeKit.Characteristics.Abstract
{
    public abstract class FloatCharacteristic : ACharacteristic
    {
        private float m_Value;

        public override string Format => "float";
        public float Value { get => m_Value; set => SetValue(value); }
        //object ACharacteristic.Value { get => Value; set => Value = (double)value; }
        public virtual Dictionary<string, int>? ValidValues { get; }
        public virtual float? MaxValue { get; }
        public virtual float? MinValue { get; }
        public virtual float? MinStep { get; }

        public delegate void ValueChange(Characteristic sender, float newValue);
        public event ValueChange? OnValueChange;

        private void SetValue(float value)
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
                value = (float)(Math.Round((double)value / (double)MinStep.Value) * (double)MinStep.Value);
            }

            m_Value = value;
        }
    }
}
