using System.Text.Json.Serialization;

namespace HomeKit
{
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
