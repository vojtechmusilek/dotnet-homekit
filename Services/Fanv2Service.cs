using System.Diagnostics.CodeAnalysis;
using HomeKit.Characteristics;

namespace HomeKit.Services
{
    public class Fanv2Service : Service
    {
        public Fanv2Service() : base("B7")
        {
            Characteristics.Add(Active);
        }

        public ActiveCharacteristic Active { get; } = new();

        public CurrentFanStateCharacteristic? CurrentFanState { get; private set; }
        public TargetFanStateCharacteristic? TargetFanState { get; private set; }
        public LockPhysicalControlsCharacteristic? LockPhysicalControls { get; private set; }
        public NameCharacteristic? Name { get; private set; }
        public RotationDirectionCharacteristic? RotationDirection { get; private set; }
        public RotationSpeedCharacteristic? RotationSpeed { get; private set; }
        public SwingModeCharacteristic? SwingMode { get; private set; }

        [MemberNotNull(nameof(CurrentFanState))]
        public void AddCurrentFanState(byte value)
        {
            CurrentFanState ??= new();
            CurrentFanState.Value = value;
            Characteristics.Add(CurrentFanState);
        }

        [MemberNotNull(nameof(TargetFanState))]
        public void AddTargetFanState(byte value)
        {
            TargetFanState ??= new();
            TargetFanState.Value = value;
            Characteristics.Add(TargetFanState);
        }

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

        [MemberNotNull(nameof(RotationDirection))]
        public void AddRotationDirection(int value)
        {
            RotationDirection ??= new();
            RotationDirection.Value = value;
            Characteristics.Add(RotationDirection);
        }

        [MemberNotNull(nameof(RotationSpeed))]
        public void AddRotationSpeed(float value)
        {
            RotationSpeed ??= new();
            RotationSpeed.Value = value;
            Characteristics.Add(RotationSpeed);
        }

        [MemberNotNull(nameof(SwingMode))]
        public void AddSwingMode(byte value)
        {
            SwingMode ??= new();
            SwingMode.Value = value;
            Characteristics.Add(SwingMode);
        }
    }
}

