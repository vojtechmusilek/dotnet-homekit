using System.Diagnostics.CodeAnalysis;
using HomeKit.Characteristics;

namespace HomeKit.Services
{
    public class WindowService : Service
    {
        public WindowService() : base("8B")
        {
            Characteristics.Add(CurrentPosition);
            Characteristics.Add(TargetPosition);
            Characteristics.Add(PositionState);
        }

        public CurrentPositionCharacteristic CurrentPosition { get; } = new();
        public TargetPositionCharacteristic TargetPosition { get; } = new();
        public PositionStateCharacteristic PositionState { get; } = new();

        public HoldPositionCharacteristic? HoldPosition { get; private set; }
        public ObstructionDetectedCharacteristic? ObstructionDetected { get; private set; }
        public NameCharacteristic? Name { get; private set; }

        [MemberNotNull(nameof(HoldPosition))]
        public void AddHoldPosition(bool value)
        {
            HoldPosition ??= new();
            HoldPosition.Value = value;
            Characteristics.Add(HoldPosition);
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

