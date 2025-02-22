using System.Diagnostics.CodeAnalysis;
using HomeKit.Characteristics;

namespace HomeKit.Services
{
    public class LockManagementService : Service
    {
        public LockManagementService() : base("44")
        {
            Characteristics.Add(LockControlPoint);
            Characteristics.Add(Version);
        }

        public LockControlPointCharacteristic LockControlPoint { get; } = new();
        public VersionCharacteristic Version { get; } = new();

        public LogsCharacteristic? Logs { get; private set; }
        public AudioFeedbackCharacteristic? AudioFeedback { get; private set; }
        public LockManagementAutoSecurityTimeoutCharacteristic? LockManagementAutoSecurityTimeout { get; private set; }
        public AdministratorOnlyAccessCharacteristic? AdministratorOnlyAccess { get; private set; }
        public LockLastKnownActionCharacteristic? LockLastKnownAction { get; private set; }
        public CurrentDoorStateCharacteristic? CurrentDoorState { get; private set; }
        public MotionDetectedCharacteristic? MotionDetected { get; private set; }
        public NameCharacteristic? Name { get; private set; }

        [MemberNotNull(nameof(Logs))]
        public void AddLogs(string value)
        {
            Logs ??= new();
            Logs.Value = value;
            Characteristics.Add(Logs);
        }

        [MemberNotNull(nameof(AudioFeedback))]
        public void AddAudioFeedback(bool value)
        {
            AudioFeedback ??= new();
            AudioFeedback.Value = value;
            Characteristics.Add(AudioFeedback);
        }

        [MemberNotNull(nameof(LockManagementAutoSecurityTimeout))]
        public void AddLockManagementAutoSecurityTimeout(uint value)
        {
            LockManagementAutoSecurityTimeout ??= new();
            LockManagementAutoSecurityTimeout.Value = value;
            Characteristics.Add(LockManagementAutoSecurityTimeout);
        }

        [MemberNotNull(nameof(AdministratorOnlyAccess))]
        public void AddAdministratorOnlyAccess(bool value)
        {
            AdministratorOnlyAccess ??= new();
            AdministratorOnlyAccess.Value = value;
            Characteristics.Add(AdministratorOnlyAccess);
        }

        [MemberNotNull(nameof(LockLastKnownAction))]
        public void AddLockLastKnownAction(byte value)
        {
            LockLastKnownAction ??= new();
            LockLastKnownAction.Value = value;
            Characteristics.Add(LockLastKnownAction);
        }

        [MemberNotNull(nameof(CurrentDoorState))]
        public void AddCurrentDoorState(byte value)
        {
            CurrentDoorState ??= new();
            CurrentDoorState.Value = value;
            Characteristics.Add(CurrentDoorState);
        }

        [MemberNotNull(nameof(MotionDetected))]
        public void AddMotionDetected(bool value)
        {
            MotionDetected ??= new();
            MotionDetected.Value = value;
            Characteristics.Add(MotionDetected);
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

