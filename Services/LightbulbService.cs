using System.Diagnostics.CodeAnalysis;
using HomeKit.Characteristics;

namespace HomeKit.Services
{
    public class LightbulbService : Service
    {
        public LightbulbService() : base("43")
        {
            Characteristics.Add(On);
        }

        public OnCharacteristic On { get; } = new();

        public BrightnessCharacteristic? Brightness { get; private set; }
        public HueCharacteristic? Hue { get; private set; }
        public SaturationCharacteristic? Saturation { get; private set; }
        public NameCharacteristic? Name { get; private set; }
        public ActiveTransitionCountCharacteristic? ActiveTransitionCount { get; private set; }
        public TransitionControlCharacteristic? TransitionControl { get; private set; }
        public SupportedTransitionConfigurationCharacteristic? SupportedTransitionConfiguration { get; private set; }

        [MemberNotNull(nameof(Brightness))]
        public void AddBrightness(int value)
        {
            Brightness ??= new();
            Brightness.Value = value;
            Characteristics.Add(Brightness);
        }

        [MemberNotNull(nameof(Hue))]
        public void AddHue(float value)
        {
            Hue ??= new();
            Hue.Value = value;
            Characteristics.Add(Hue);
        }

        [MemberNotNull(nameof(Saturation))]
        public void AddSaturation(float value)
        {
            Saturation ??= new();
            Saturation.Value = value;
            Characteristics.Add(Saturation);
        }

        [MemberNotNull(nameof(Name))]
        public void AddName(string value)
        {
            Name ??= new();
            Name.Value = value;
            Characteristics.Add(Name);
        }

        [MemberNotNull(nameof(ActiveTransitionCount))]
        public void AddActiveTransitionCount(byte value)
        {
            ActiveTransitionCount ??= new();
            ActiveTransitionCount.Value = value;
            Characteristics.Add(ActiveTransitionCount);
        }

        [MemberNotNull(nameof(TransitionControl))]
        public void AddTransitionControl(string value)
        {
            TransitionControl ??= new();
            TransitionControl.Value = value;
            Characteristics.Add(TransitionControl);
        }

        [MemberNotNull(nameof(SupportedTransitionConfiguration))]
        public void AddSupportedTransitionConfiguration(string value)
        {
            SupportedTransitionConfiguration ??= new();
            SupportedTransitionConfiguration.Value = value;
            Characteristics.Add(SupportedTransitionConfiguration);
        }
    }
}

