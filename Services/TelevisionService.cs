using System.Diagnostics.CodeAnalysis;
using HomeKit.Characteristics;

namespace HomeKit.Services
{
    public class TelevisionService : Service
    {
        public TelevisionService() : base("D8")
        {
            Characteristics.Add(Active);
            Characteristics.Add(ActiveIdentifier);
            Characteristics.Add(ConfiguredName);
            Characteristics.Add(SleepDiscoveryMode);
        }

        public ActiveCharacteristic Active { get; } = new();
        public ActiveIdentifierCharacteristic ActiveIdentifier { get; } = new();
        public ConfiguredNameCharacteristic ConfiguredName { get; } = new();
        public SleepDiscoveryModeCharacteristic SleepDiscoveryMode { get; } = new();

        public BrightnessCharacteristic? Brightness { get; private set; }
        public ClosedCaptionsCharacteristic? ClosedCaptions { get; private set; }
        public DisplayOrderCharacteristic? DisplayOrder { get; private set; }
        public CurrentMediaStateCharacteristic? CurrentMediaState { get; private set; }
        public TargetMediaStateCharacteristic? TargetMediaState { get; private set; }
        public PictureModeCharacteristic? PictureMode { get; private set; }
        public PowerModeSelectionCharacteristic? PowerModeSelection { get; private set; }
        public RemoteKeyCharacteristic? RemoteKey { get; private set; }

        [MemberNotNull(nameof(Brightness))]
        public void AddBrightness(int value)
        {
            Brightness ??= new();
            Brightness.Value = value;
            Characteristics.Add(Brightness);
        }

        [MemberNotNull(nameof(ClosedCaptions))]
        public void AddClosedCaptions(byte value)
        {
            ClosedCaptions ??= new();
            ClosedCaptions.Value = value;
            Characteristics.Add(ClosedCaptions);
        }

        [MemberNotNull(nameof(DisplayOrder))]
        public void AddDisplayOrder(string value)
        {
            DisplayOrder ??= new();
            DisplayOrder.Value = value;
            Characteristics.Add(DisplayOrder);
        }

        [MemberNotNull(nameof(CurrentMediaState))]
        public void AddCurrentMediaState(byte value)
        {
            CurrentMediaState ??= new();
            CurrentMediaState.Value = value;
            Characteristics.Add(CurrentMediaState);
        }

        [MemberNotNull(nameof(TargetMediaState))]
        public void AddTargetMediaState(byte value)
        {
            TargetMediaState ??= new();
            TargetMediaState.Value = value;
            Characteristics.Add(TargetMediaState);
        }

        [MemberNotNull(nameof(PictureMode))]
        public void AddPictureMode(byte value)
        {
            PictureMode ??= new();
            PictureMode.Value = value;
            Characteristics.Add(PictureMode);
        }

        [MemberNotNull(nameof(PowerModeSelection))]
        public void AddPowerModeSelection(byte value)
        {
            PowerModeSelection ??= new();
            PowerModeSelection.Value = value;
            Characteristics.Add(PowerModeSelection);
        }

        [MemberNotNull(nameof(RemoteKey))]
        public void AddRemoteKey(byte value)
        {
            RemoteKey ??= new();
            RemoteKey.Value = value;
            Characteristics.Add(RemoteKey);
        }
    }
}

