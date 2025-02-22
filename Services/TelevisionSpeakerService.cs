using System.Diagnostics.CodeAnalysis;
using HomeKit.Characteristics;

namespace HomeKit.Services
{
    public class TelevisionSpeakerService : Service
    {
        public TelevisionSpeakerService() : base("13")
        {
            Characteristics.Add(Mute);
        }

        public MuteCharacteristic Mute { get; } = new();

        public ActiveCharacteristic? Active { get; private set; }
        public VolumeCharacteristic? Volume { get; private set; }
        public VolumeControlTypeCharacteristic? VolumeControlType { get; private set; }
        public VolumeSelectorCharacteristic? VolumeSelector { get; private set; }
        public NameCharacteristic? Name { get; private set; }

        [MemberNotNull(nameof(Active))]
        public void AddActive(byte value)
        {
            Active ??= new();
            Active.Value = value;
            Characteristics.Add(Active);
        }

        [MemberNotNull(nameof(Volume))]
        public void AddVolume(byte value)
        {
            Volume ??= new();
            Volume.Value = value;
            Characteristics.Add(Volume);
        }

        [MemberNotNull(nameof(VolumeControlType))]
        public void AddVolumeControlType(byte value)
        {
            VolumeControlType ??= new();
            VolumeControlType.Value = value;
            Characteristics.Add(VolumeControlType);
        }

        [MemberNotNull(nameof(VolumeSelector))]
        public void AddVolumeSelector(byte value)
        {
            VolumeSelector ??= new();
            VolumeSelector.Value = value;
            Characteristics.Add(VolumeSelector);
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

