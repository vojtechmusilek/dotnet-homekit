using System.Diagnostics.CodeAnalysis;
using HomeKit.Characteristics;

namespace HomeKit.Services
{
    public class BatteryServiceService : Service
    {
        public BatteryServiceService() : base("96")
        {
            Characteristics.Add(BatteryLevel);
            Characteristics.Add(ChargingState);
            Characteristics.Add(StatusLowBattery);
        }

        public BatteryLevelCharacteristic BatteryLevel { get; } = new();
        public ChargingStateCharacteristic ChargingState { get; } = new();
        public StatusLowBatteryCharacteristic StatusLowBattery { get; } = new();

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

