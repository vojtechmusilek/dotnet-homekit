using System.Diagnostics.CodeAnalysis;
using HomeKit.Characteristics;

namespace HomeKit.Services
{
    public class LockMechanismService : Service
    {
        public LockMechanismService() : base("45")
        {
            Characteristics.Add(LockCurrentState);
            Characteristics.Add(LockTargetState);
        }

        public LockCurrentStateCharacteristic LockCurrentState { get; } = new();
        public LockTargetStateCharacteristic LockTargetState { get; } = new();

        public NameCharacteristic? Name { get; private set; }

        [MemberNotNull(nameof(Name))]
        public void AddName(string value)
        {
            Name ??= new();
            Name.Value = value;
            Characteristics.Add(Name);
        }
    }
}

