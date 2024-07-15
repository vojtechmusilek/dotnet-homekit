using System.Text.Json.Serialization;

namespace HomeKit
{
    public interface ICharacteristic
    {
        [JsonIgnore]
        public int Aid { get; set; }
        public int Iid { get; set; }
        public string Type { get; }
        public string Format { get; }
        public string[] Perms { get; }
        public object Value { get; set; }
    }
}
