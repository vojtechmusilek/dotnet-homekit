using System.Diagnostics.CodeAnalysis;
using HomeKit.Characteristics;

namespace HomeKit.Services
{
    public class ValveService : Service
    {
        public ValveService() : base("D0")
        {
            Characteristics.Add(Active);
            Characteristics.Add(InUse);
            Characteristics.Add(ValveType);
        }

        public ActiveCharacteristic Active { get; } = new();
        public InUseCharacteristic InUse { get; } = new();
        public ValveTypeCharacteristic ValveType { get; } = new();

        public SetDurationCharacteristic? SetDuration { get; private set; }
        public RemainingDurationCharacteristic? RemainingDuration { get; private set; }
        public IsConfiguredCharacteristic? IsConfigured { get; private set; }
        public ServiceLabelIndexCharacteristic? ServiceLabelIndex { get; private set; }
        public StatusFaultCharacteristic? StatusFault { get; private set; }
        public NameCharacteristic? Name { get; private set; }

        [MemberNotNull(nameof(SetDuration))]
        public void AddSetDuration(uint value)
        {
            SetDuration ??= new();
            SetDuration.Value = value;
            Characteristics.Add(SetDuration);
        }

        [MemberNotNull(nameof(RemainingDuration))]
        public void AddRemainingDuration(uint value)
        {
            RemainingDuration ??= new();
            RemainingDuration.Value = value;
            Characteristics.Add(RemainingDuration);
        }

        [MemberNotNull(nameof(IsConfigured))]
        public void AddIsConfigured(byte value)
        {
            IsConfigured ??= new();
            IsConfigured.Value = value;
            Characteristics.Add(IsConfigured);
        }

        [MemberNotNull(nameof(ServiceLabelIndex))]
        public void AddServiceLabelIndex(byte value)
        {
            ServiceLabelIndex ??= new();
            ServiceLabelIndex.Value = value;
            Characteristics.Add(ServiceLabelIndex);
        }

        [MemberNotNull(nameof(StatusFault))]
        public void AddStatusFault(byte value)
        {
            StatusFault ??= new();
            StatusFault.Value = value;
            Characteristics.Add(StatusFault);
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

