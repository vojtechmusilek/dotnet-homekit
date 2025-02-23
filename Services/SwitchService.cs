using System.Diagnostics.CodeAnalysis;
using HomeKit.Characteristics;

namespace HomeKit.Services
{
    public class SwitchService : Service
    {
        public SwitchService() : base("49")
        {
            Characteristics.Add(On);
        }

        public OnCharacteristic On { get; } = new();

        public NameCharacteristic? Name { get; private set; }

        [MemberNotNull(nameof(Name))]
        public void AddName(string value)
        {
            Name ??= new();
            Name.Value = value;
            Characteristics.Add(Name);
        }
    }
}

