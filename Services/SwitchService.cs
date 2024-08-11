using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;
using HomeKit.Characteristics;

namespace HomeKit.Services
{
    public class SwitchService : IService
    {
        public int Iid { get; set; }
        public string Type => "49";
        public List<ICharacteristic> Characteristics => [Name, On];

        [JsonIgnore] public NameCharacteristic? Name { get; private set; }
        [JsonIgnore] public OnCharacteristic On { get; } = new();

        [MemberNotNull(nameof(Name))]
        public void AddName()
        {
            Name ??= new();
        }
    }
}
