using System.Diagnostics.CodeAnalysis;
using HomeKit.Characteristics;

namespace HomeKit.Services
{
    public class SecuritySystemService : Service
    {
        public SecuritySystemService() : base("7E")
        {
            Characteristics.Add(SecuritySystemCurrentState);
            Characteristics.Add(SecuritySystemTargetState);
        }

        public SecuritySystemCurrentStateCharacteristic SecuritySystemCurrentState { get; } = new();
        public SecuritySystemTargetStateCharacteristic SecuritySystemTargetState { get; } = new();

        public StatusFaultCharacteristic? StatusFault { get; private set; }
        public StatusTamperedCharacteristic? StatusTampered { get; private set; }
        public SecuritySystemAlarmTypeCharacteristic? SecuritySystemAlarmType { get; private set; }
        public NameCharacteristic? Name { get; private set; }

        [MemberNotNull(nameof(StatusFault))]
        public void AddStatusFault(byte value)
        {
            StatusFault ??= new();
            StatusFault.Value = value;
            Characteristics.Add(StatusFault);
        }

        [MemberNotNull(nameof(StatusTampered))]
        public void AddStatusTampered(byte value)
        {
            StatusTampered ??= new();
            StatusTampered.Value = value;
            Characteristics.Add(StatusTampered);
        }

        [MemberNotNull(nameof(SecuritySystemAlarmType))]
        public void AddSecuritySystemAlarmType(byte value)
        {
            SecuritySystemAlarmType ??= new();
            SecuritySystemAlarmType.Value = value;
            Characteristics.Add(SecuritySystemAlarmType);
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

