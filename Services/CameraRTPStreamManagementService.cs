using System.Diagnostics.CodeAnalysis;
using HomeKit.Characteristics;

namespace HomeKit.Services
{
    public class CameraRTPStreamManagementService : Service
    {
        public CameraRTPStreamManagementService() : base("10")
        {
            Characteristics.Add(SupportedVideoStreamConfiguration);
            Characteristics.Add(SupportedAudioStreamConfiguration);
            Characteristics.Add(SupportedRTPConfiguration);
            Characteristics.Add(SelectedRTPStreamConfiguration);
            Characteristics.Add(StreamingStatus);
            Characteristics.Add(SetupEndpoints);
        }

        public SupportedVideoStreamConfigurationCharacteristic SupportedVideoStreamConfiguration { get; } = new();
        public SupportedAudioStreamConfigurationCharacteristic SupportedAudioStreamConfiguration { get; } = new();
        public SupportedRTPConfigurationCharacteristic SupportedRTPConfiguration { get; } = new();
        public SelectedRTPStreamConfigurationCharacteristic SelectedRTPStreamConfiguration { get; } = new();
        public StreamingStatusCharacteristic StreamingStatus { get; } = new();
        public SetupEndpointsCharacteristic SetupEndpoints { get; } = new();

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

