using System.Diagnostics.CodeAnalysis;
using HomeKit.Characteristics;

namespace HomeKit.Services
{
    public class OccupancySensorService : Service
    {
        public OccupancySensorService() : base("86")
        {
            Characteristics.Add(OccupancyDetected);
        }

        public OccupancyDetectedCharacteristic OccupancyDetected { get; } = new();

        public NameCharacteristic? Name { get; private set; }
        public StatusActiveCharacteristic? StatusActive { get; private set; }
        public StatusFaultCharacteristic? StatusFault { get; private set; }
        public StatusTamperedCharacteristic? StatusTampered { get; private set; }
        public StatusLowBatteryCharacteristic? StatusLowBattery { get; private set; }

        [MemberNotNull(nameof(Name))]
        public void AddName(string value)
        {
            Name ??= new();
            Name.Value = value;
            Characteristics.Add(Name);
        }

        [MemberNotNull(nameof(StatusActive))]
        public void AddStatusActive(bool value)
        {
            StatusActive ??= new();
            StatusActive.Value = value;
            Characteristics.Add(StatusActive);
        }

        [MemberNotNull(nameof(StatusFault))]
        public void AddStatusFault(byte value)
        {
            StatusFault ??= new();
            StatusFault.Value = value;
            Characteristics.Add(StatusFault);
        }

        [MemberNotNull(nameof(StatusTampered))]
        public void AddStatusTampered(byte value)
        {
            StatusTampered ??= new();
            StatusTampered.Value = value;
            Characteristics.Add(StatusTampered);
        }

        [MemberNotNull(nameof(StatusLowBattery))]
        public void AddStatusLowBattery(byte value)
        {
            StatusLowBattery ??= new();
            StatusLowBattery.Value = value;
            Characteristics.Add(StatusLowBattery);
        }
    }
}

