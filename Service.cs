using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace HomeKit
{
    public abstract class Service(string type)
    {
        [JsonInclude]
        internal int Iid { get; set; }
        public string Type { get; } = type;
        public List<Characteristic> Characteristics { get; } = new();
    }
}
