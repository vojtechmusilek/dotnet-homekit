using System.Diagnostics.CodeAnalysis;
using HomeKit.Characteristics;

namespace HomeKit.Services
{
    public class AirPurifierService : Service
    {
        public AirPurifierService() : base("BB")
        {
            Characteristics.Add(Active);
            Characteristics.Add(CurrentAirPurifierState);
            Characteristics.Add(TargetAirPurifierState);
        }

        public ActiveCharacteristic Active { get; } = new();
        public CurrentAirPurifierStateCharacteristic CurrentAirPurifierState { get; } = new();
        public TargetAirPurifierStateCharacteristic TargetAirPurifierState { get; } = new();

        public LockPhysicalControlsCharacteristic? LockPhysicalControls { get; private set; }
        public NameCharacteristic? Name { get; private set; }
        public SwingModeCharacteristic? SwingMode { get; private set; }
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

        [MemberNotNull(nameof(RotationSpeed))]
        public void AddRotationSpeed(float value)
        {
            RotationSpeed ??= new();
            RotationSpeed.Value = value;
            Characteristics.Add(RotationSpeed);
        }
    }
}

