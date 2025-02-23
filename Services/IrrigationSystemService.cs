using System.Diagnostics.CodeAnalysis;
using HomeKit.Characteristics;

namespace HomeKit.Services
{
    public class IrrigationSystemService : Service
    {
        public IrrigationSystemService() : base("CF")
        {
            Characteristics.Add(Active);
            Characteristics.Add(ProgramMode);
            Characteristics.Add(InUse);
        }

        public ActiveCharacteristic Active { get; } = new();
        public ProgramModeCharacteristic ProgramMode { get; } = new();
        public InUseCharacteristic InUse { get; } = new();

        public NameCharacteristic? Name { get; private set; }
        public RemainingDurationCharacteristic? RemainingDuration { get; private set; }
        public StatusFaultCharacteristic? StatusFault { get; private set; }

        [MemberNotNull(nameof(Name))]
        public void AddName(string value)
        {
            Name ??= new();
            Name.Value = value;
            Characteristics.Add(Name);
        }

        [MemberNotNull(nameof(RemainingDuration))]
        public void AddRemainingDuration(uint value)
        {
            RemainingDuration ??= new();
            RemainingDuration.Value = value;
            Characteristics.Add(RemainingDuration);
        }

        [MemberNotNull(nameof(StatusFault))]
        public void AddStatusFault(byte value)
        {
            StatusFault ??= new();
            StatusFault.Value = value;
            Characteristics.Add(StatusFault);
        }
    }
}

