using System.Diagnostics.CodeAnalysis;
using HomeKit.Characteristics;

namespace HomeKit.Services
{
    public class MicrophoneService : Service
    {
        public MicrophoneService() : base("12")
        {
            Characteristics.Add(Volume);
            Characteristics.Add(Mute);
        }

        public VolumeCharacteristic Volume { get; } = new();
        public MuteCharacteristic Mute { get; } = new();

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

