using System;
using System.Collections.Generic;
using HomeKit.Resources;

namespace HomeKit
{
    public class Characteristic
    {
        public int Iid { get; }
        public string Format { get; }
        public string Type { get; }
        public List<string> Perms { get; }
        public object Value { get; }

        public Characteristic(CharacteristicType type)
        {

        }

        public void SetValue(object value)
        {
            throw new NotImplementedException();
        }
    }
}