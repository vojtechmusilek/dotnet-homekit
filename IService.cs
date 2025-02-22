using System.Collections.Generic;
using HomeKit.Characteristics.Abstract;

namespace HomeKit
{
    public abstract class AService(string type)
    {
        public int Iid { get; set; }
        public string Type { get; } = type;
        public List<ACharacteristic> Characteristics { get; } = new();
    }
}
