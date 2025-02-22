using System.Diagnostics.CodeAnalysis;
using HomeKit.Characteristics;

namespace HomeKit.Services
{
    public class DoorbellService : Service
    {
        public DoorbellService() : base("21")
        {
            Characteristics.Add(ProgrammableSwitchEvent);
        }

        public ProgrammableSwitchEventCharacteristic ProgrammableSwitchEvent { get; } = new();

        public BrightnessCharacteristic? Brightness { get; private set; }
        public VolumeCharacteristic? Volume { get; private set; }
        public NameCharacteristic? Name { get; private set; }

        [MemberNotNull(nameof(Brightness))]
        public void AddBrightness(int value)
        {
            Brightness ??= new();
            Brightness.Value = value;
            Characteristics.Add(Brightness);
        }

        [MemberNotNull(nameof(Volume))]
        public void AddVolume(byte value)
        {
            Volume ??= new();
            Volume.Value = value;
            Characteristics.Add(Volume);
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

