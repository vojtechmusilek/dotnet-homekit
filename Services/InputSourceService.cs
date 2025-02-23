using System.Diagnostics.CodeAnalysis;
using HomeKit.Characteristics;

namespace HomeKit.Services
{
    public class InputSourceService : Service
    {
        public InputSourceService() : base("D9")
        {
            Characteristics.Add(ConfiguredName);
            Characteristics.Add(InputSourceType);
            Characteristics.Add(IsConfigured);
            Characteristics.Add(CurrentVisibilityState);
        }

        public ConfiguredNameCharacteristic ConfiguredName { get; } = new();
        public InputSourceTypeCharacteristic InputSourceType { get; } = new();
        public IsConfiguredCharacteristic IsConfigured { get; } = new();
        public CurrentVisibilityStateCharacteristic CurrentVisibilityState { get; } = new();

        public IdentifierCharacteristic? Identifier { get; private set; }
        public InputDeviceTypeCharacteristic? InputDeviceType { get; private set; }
        public TargetVisibilityStateCharacteristic? TargetVisibilityState { get; private set; }
        public NameCharacteristic? Name { get; private set; }

        [MemberNotNull(nameof(Identifier))]
        public void AddIdentifier(uint value)
        {
            Identifier ??= new();
            Identifier.Value = value;
            Characteristics.Add(Identifier);
        }

        [MemberNotNull(nameof(InputDeviceType))]
        public void AddInputDeviceType(byte value)
        {
            InputDeviceType ??= new();
            InputDeviceType.Value = value;
            Characteristics.Add(InputDeviceType);
        }

        [MemberNotNull(nameof(TargetVisibilityState))]
        public void AddTargetVisibilityState(byte value)
        {
            TargetVisibilityState ??= new();
            TargetVisibilityState.Value = value;
            Characteristics.Add(TargetVisibilityState);
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

