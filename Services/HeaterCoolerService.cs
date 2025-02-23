using System.Diagnostics.CodeAnalysis;
using HomeKit.Characteristics;

namespace HomeKit.Services
{
    public class HeaterCoolerService : Service
    {
        public HeaterCoolerService() : base("BC")
        {
            Characteristics.Add(Active);
            Characteristics.Add(CurrentHeaterCoolerState);
            Characteristics.Add(TargetHeaterCoolerState);
            Characteristics.Add(CurrentTemperature);
        }

        public ActiveCharacteristic Active { get; } = new();
        public CurrentHeaterCoolerStateCharacteristic CurrentHeaterCoolerState { get; } = new();
        public TargetHeaterCoolerStateCharacteristic TargetHeaterCoolerState { get; } = new();
        public CurrentTemperatureCharacteristic CurrentTemperature { get; } = new();

        public LockPhysicalControlsCharacteristic? LockPhysicalControls { get; private set; }
        public NameCharacteristic? Name { get; private set; }
        public SwingModeCharacteristic? SwingMode { get; private set; }
        public CoolingThresholdTemperatureCharacteristic? CoolingThresholdTemperature { get; private set; }
        public HeatingThresholdTemperatureCharacteristic? HeatingThresholdTemperature { get; private set; }
        public TemperatureDisplayUnitsCharacteristic? TemperatureDisplayUnits { get; private set; }
        public RotationSpeedCharacteristic? RotationSpeed { get; private set; }

        [MemberNotNull(nameof(LockPhysicalControls))]
        public void AddLockPhysicalControls(byte value)
        {
            LockPhysicalControls ??= new();
            LockPhysicalControls.Value = value;
            Characteristics.Add(LockPhysicalControls);
        }

        [MemberNotNull(nameof(Name))]
        public void AddName(string value)
        {
            Name ??= new();
            Name.Value = value;
            Characteristics.Add(Name);
        }

        [MemberNotNull(nameof(SwingMode))]
        public void AddSwingMode(byte value)
        {
            SwingMode ??= new();
            SwingMode.Value = value;
            Characteristics.Add(SwingMode);
        }

        [MemberNotNull(nameof(CoolingThresholdTemperature))]
        public void AddCoolingThresholdTemperature(float value)
        {
            CoolingThresholdTemperature ??= new();
            CoolingThresholdTemperature.Value = value;
            Characteristics.Add(CoolingThresholdTemperature);
        }

        [MemberNotNull(nameof(HeatingThresholdTemperature))]
        public void AddHeatingThresholdTemperature(float value)
        {
            HeatingThresholdTemperature ??= new();
            HeatingThresholdTemperature.Value = value;
            Characteristics.Add(HeatingThresholdTemperature);
        }

        [MemberNotNull(nameof(TemperatureDisplayUnits))]
        public void AddTemperatureDisplayUnits(byte value)
        {
            TemperatureDisplayUnits ??= new();
            TemperatureDisplayUnits.Value = value;
            Characteristics.Add(TemperatureDisplayUnits);
        }

        [MemberNotNull(nameof(RotationSpeed))]
        public void AddRotationSpeed(float value)
        {
            RotationSpeed ??= new();
            RotationSpeed.Value = value;
            Characteristics.Add(RotationSpeed);
        }
    }
}

