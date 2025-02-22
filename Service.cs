using System.Collections.Generic;

namespace HomeKit
{
    public abstract class Service(string type)
    {
        public int Iid { get; set; }
        public string Type { get; } = type;
        public List<Characteristic> Characteristics { get; } = new();
    }
}
