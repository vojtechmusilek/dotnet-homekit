using System.Diagnostics.CodeAnalysis;
using HomeKit.Characteristics;

namespace HomeKit.Services
{
    public class SpeakerService : Service
    {
        public SpeakerService() : base("13")
        {
            Characteristics.Add(Mute);
        }

        public MuteCharacteristic Mute { get; } = new();

        public NameCharacteristic? Name { get; private set; }
        public VolumeCharacteristic? Volume { get; private set; }

        [MemberNotNull(nameof(Name))]
        public void AddName(string value)
        {
            Name ??= new();
            Name.Value = value;
            Characteristics.Add(Name);
        }

        [MemberNotNull(nameof(Volume))]
        public void AddVolume(byte value)
        {
            Volume ??= new();
            Volume.Value = value;
            Characteristics.Add(Volume);
        }
    }
}

