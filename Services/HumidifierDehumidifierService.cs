using System.Diagnostics.CodeAnalysis;
using HomeKit.Characteristics;

namespace HomeKit.Services
{
    public class HumidifierDehumidifierService : Service
    {
        public HumidifierDehumidifierService() : base("BD")
        {
            Characteristics.Add(CurrentRelativeHumidity);
            Characteristics.Add(CurrentHumidifierDehumidifierState);
            Characteristics.Add(TargetHumidifierDehumidifierState);
            Characteristics.Add(Active);
        }

        public CurrentRelativeHumidityCharacteristic CurrentRelativeHumidity { get; } = new();
        public CurrentHumidifierDehumidifierStateCharacteristic CurrentHumidifierDehumidifierState { get; } = new();
        public TargetHumidifierDehumidifierStateCharacteristic TargetHumidifierDehumidifierState { get; } = new();
        public ActiveCharacteristic Active { get; } = new();

        public LockPhysicalControlsCharacteristic? LockPhysicalControls { get; private set; }
        public NameCharacteristic? Name { get; private set; }
        public SwingModeCharacteristic? SwingMode { get; private set; }
        public WaterLevelCharacteristic? WaterLevel { get; private set; }
        public RelativeHumidityDehumidifierThresholdCharacteristic? RelativeHumidityDehumidifierThreshold { get; private set; }
        public RelativeHumidityHumidifierThresholdCharacteristic? RelativeHumidityHumidifierThreshold { get; private set; }
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

        [MemberNotNull(nameof(WaterLevel))]
        public void AddWaterLevel(float value)
        {
            WaterLevel ??= new();
            WaterLevel.Value = value;
            Characteristics.Add(WaterLevel);
        }

        [MemberNotNull(nameof(RelativeHumidityDehumidifierThreshold))]
        public void AddRelativeHumidityDehumidifierThreshold(float value)
        {
            RelativeHumidityDehumidifierThreshold ??= new();
            RelativeHumidityDehumidifierThreshold.Value = value;
            Characteristics.Add(RelativeHumidityDehumidifierThreshold);
        }

        [MemberNotNull(nameof(RelativeHumidityHumidifierThreshold))]
        public void AddRelativeHumidityHumidifierThreshold(float value)
        {
            RelativeHumidityHumidifierThreshold ??= new();
            RelativeHumidityHumidifierThreshold.Value = value;
            Characteristics.Add(RelativeHumidityHumidifierThreshold);
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

