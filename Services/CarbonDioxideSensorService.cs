using System.Diagnostics.CodeAnalysis;
using HomeKit.Characteristics;

namespace HomeKit.Services
{
    public class CarbonDioxideSensorService : Service
    {
        public CarbonDioxideSensorService() : base("97")
        {
            Characteristics.Add(CarbonDioxideDetected);
        }

        public CarbonDioxideDetectedCharacteristic CarbonDioxideDetected { get; } = new();

        public StatusActiveCharacteristic? StatusActive { get; private set; }
        public StatusFaultCharacteristic? StatusFault { get; private set; }
        public StatusLowBatteryCharacteristic? StatusLowBattery { get; private set; }
        public StatusTamperedCharacteristic? StatusTampered { get; private set; }
        public CarbonDioxideLevelCharacteristic? CarbonDioxideLevel { get; private set; }
        public CarbonDioxidePeakLevelCharacteristic? CarbonDioxidePeakLevel { get; private set; }
        public NameCharacteristic? Name { get; private set; }

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

        [MemberNotNull(nameof(StatusLowBattery))]
        public void AddStatusLowBattery(byte value)
        {
            StatusLowBattery ??= new();
            StatusLowBattery.Value = value;
            Characteristics.Add(StatusLowBattery);
        }

        [MemberNotNull(nameof(StatusTampered))]
        public void AddStatusTampered(byte value)
        {
            StatusTampered ??= new();
            StatusTampered.Value = value;
            Characteristics.Add(StatusTampered);
        }

        [MemberNotNull(nameof(CarbonDioxideLevel))]
        public void AddCarbonDioxideLevel(float value)
        {
            CarbonDioxideLevel ??= new();
            CarbonDioxideLevel.Value = value;
            Characteristics.Add(CarbonDioxideLevel);
        }

        [MemberNotNull(nameof(CarbonDioxidePeakLevel))]
        public void AddCarbonDioxidePeakLevel(float value)
        {
            CarbonDioxidePeakLevel ??= new();
            CarbonDioxidePeakLevel.Value = value;
            Characteristics.Add(CarbonDioxidePeakLevel);
        }

        [MemberNotNull(nameof(Name))]
        public void AddName(string value)
        {
            Name ??= new();
            Name.Value = value;
            Characteristics.Add(Name);
        }
    }
}

