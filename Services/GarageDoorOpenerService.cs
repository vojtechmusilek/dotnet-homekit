using System.Diagnostics.CodeAnalysis;
using HomeKit.Characteristics;

namespace HomeKit.Services
{
    public class GarageDoorOpenerService : Service
    {
        public GarageDoorOpenerService() : base("41")
        {
            Characteristics.Add(CurrentDoorState);
            Characteristics.Add(TargetDoorState);
            Characteristics.Add(ObstructionDetected);
        }

        public CurrentDoorStateCharacteristic CurrentDoorState { get; } = new();
        public TargetDoorStateCharacteristic TargetDoorState { get; } = new();
        public ObstructionDetectedCharacteristic ObstructionDetected { get; } = new();

        public LockCurrentStateCharacteristic? LockCurrentState { get; private set; }
        public LockTargetStateCharacteristic? LockTargetState { get; private set; }
        public NameCharacteristic? Name { get; private set; }

        [MemberNotNull(nameof(LockCurrentState))]
        public void AddLockCurrentState(byte value)
        {
            LockCurrentState ??= new();
            LockCurrentState.Value = value;
            Characteristics.Add(LockCurrentState);
        }

        [MemberNotNull(nameof(LockTargetState))]
        public void AddLockTargetState(byte value)
        {
            LockTargetState ??= new();
            LockTargetState.Value = value;
            Characteristics.Add(LockTargetState);
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

