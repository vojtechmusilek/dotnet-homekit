using System.Diagnostics.CodeAnalysis;
using HomeKit.Characteristics;

namespace HomeKit.Services
{
    public class FanService : Service
    {
        public FanService() : base("40")
        {
            Characteristics.Add(On);
        }

        public OnCharacteristic On { get; } = new();

        public RotationDirectionCharacteristic? RotationDirection { get; private set; }
        public RotationSpeedCharacteristic? RotationSpeed { get; private set; }
        public NameCharacteristic? Name { get; private set; }

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

        [MemberNotNull(nameof(Name))]
        public void AddName(string value)
        {
            Name ??= new();
            Name.Value = value;
            Characteristics.Add(Name);
        }
    }
}

