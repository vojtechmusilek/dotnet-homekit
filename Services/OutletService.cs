using System.Diagnostics.CodeAnalysis;
using HomeKit.Characteristics;

namespace HomeKit.Services
{
    public class OutletService : Service
    {
        public OutletService() : base("47")
        {
            Characteristics.Add(On);
            Characteristics.Add(OutletInUse);
        }

        public OnCharacteristic On { get; } = new();
        public OutletInUseCharacteristic OutletInUse { get; } = new();

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

