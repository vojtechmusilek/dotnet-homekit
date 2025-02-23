using System.Diagnostics.CodeAnalysis;
using HomeKit.Characteristics;

namespace HomeKit.Services
{
    public class SlatService : Service
    {
        public SlatService() : base("B9")
        {
            Characteristics.Add(SlatType);
            Characteristics.Add(CurrentSlatState);
        }

        public SlatTypeCharacteristic SlatType { get; } = new();
        public CurrentSlatStateCharacteristic CurrentSlatState { get; } = new();

        public NameCharacteristic? Name { get; private set; }
        public CurrentTiltAngleCharacteristic? CurrentTiltAngle { get; private set; }
        public TargetTiltAngleCharacteristic? TargetTiltAngle { get; private set; }
        public SwingModeCharacteristic? SwingMode { get; private set; }

        [MemberNotNull(nameof(Name))]
        public void AddName(string value)
        {
            Name ??= new();
            Name.Value = value;
            Characteristics.Add(Name);
        }

        [MemberNotNull(nameof(CurrentTiltAngle))]
        public void AddCurrentTiltAngle(int value)
        {
            CurrentTiltAngle ??= new();
            CurrentTiltAngle.Value = value;
            Characteristics.Add(CurrentTiltAngle);
        }

        [MemberNotNull(nameof(TargetTiltAngle))]
        public void AddTargetTiltAngle(int value)
        {
            TargetTiltAngle ??= new();
            TargetTiltAngle.Value = value;
            Characteristics.Add(TargetTiltAngle);
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

