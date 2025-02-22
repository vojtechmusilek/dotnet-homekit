using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;
using HomeKit.Characteristics;

namespace HomeKit.Services
{
    public class SwitchService : AService
    {
        public SwitchService() : base("49")
        {
            Characteristics.Add(On);
        }

        [JsonIgnore] public OnCharacteristic On { get; } = new();

        [JsonIgnore] public NameCharacteristic? Name { get; private set; }

        [MemberNotNull(nameof(Name))]
        public void AddName(string value)
        {
            Name ??= new();
            Name.Value = value;
            Characteristics.Add(Name);
        }
    }
}
