using System.Diagnostics.CodeAnalysis;
using HomeKit.Characteristics;

namespace HomeKit.Services
{
    public class WindowCoveringService : Service
    {
        public WindowCoveringService() : base("8C")
        {
            Characteristics.Add(CurrentPosition);
            Characteristics.Add(TargetPosition);
            Characteristics.Add(PositionState);
        }

        public CurrentPositionCharacteristic CurrentPosition { get; } = new();
        public TargetPositionCharacteristic TargetPosition { get; } = new();
        public PositionStateCharacteristic PositionState { get; } = new();

        public HoldPositionCharacteristic? HoldPosition { get; private set; }
        public TargetHorizontalTiltAngleCharacteristic? TargetHorizontalTiltAngle { get; private set; }
        public TargetVerticalTiltAngleCharacteristic? TargetVerticalTiltAngle { get; private set; }
        public CurrentHorizontalTiltAngleCharacteristic? CurrentHorizontalTiltAngle { get; private set; }
        public CurrentVerticalTiltAngleCharacteristic? CurrentVerticalTiltAngle { get; private set; }
        public ObstructionDetectedCharacteristic? ObstructionDetected { get; private set; }
        public NameCharacteristic? Name { get; private set; }

        [MemberNotNull(nameof(HoldPosition))]
        public void AddHoldPosition(bool value)
        {
            HoldPosition ??= new();
            HoldPosition.Value = value;
            Characteristics.Add(HoldPosition);
        }

        [MemberNotNull(nameof(TargetHorizontalTiltAngle))]
        public void AddTargetHorizontalTiltAngle(int value)
        {
            TargetHorizontalTiltAngle ??= new();
            TargetHorizontalTiltAngle.Value = value;
            Characteristics.Add(TargetHorizontalTiltAngle);
        }

        [MemberNotNull(nameof(TargetVerticalTiltAngle))]
        public void AddTargetVerticalTiltAngle(int value)
        {
            TargetVerticalTiltAngle ??= new();
            TargetVerticalTiltAngle.Value = value;
            Characteristics.Add(TargetVerticalTiltAngle);
        }

        [MemberNotNull(nameof(CurrentHorizontalTiltAngle))]
        public void AddCurrentHorizontalTiltAngle(int value)
        {
            CurrentHorizontalTiltAngle ??= new();
            CurrentHorizontalTiltAngle.Value = value;
            Characteristics.Add(CurrentHorizontalTiltAngle);
        }

        [MemberNotNull(nameof(CurrentVerticalTiltAngle))]
        public void AddCurrentVerticalTiltAngle(int value)
        {
            CurrentVerticalTiltAngle ??= new();
            CurrentVerticalTiltAngle.Value = value;
            Characteristics.Add(CurrentVerticalTiltAngle);
        }

        [MemberNotNull(nameof(ObstructionDetected))]
        public void AddObstructionDetected(bool value)
        {
            ObstructionDetected ??= new();
            ObstructionDetected.Value = value;
            Characteristics.Add(ObstructionDetected);
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

