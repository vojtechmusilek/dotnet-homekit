using System.Diagnostics.CodeAnalysis;
using HomeKit.Characteristics;

namespace HomeKit.Services
{
    public class StatelessProgrammableSwitchService : Service
    {
        public StatelessProgrammableSwitchService() : base("89")
        {
            Characteristics.Add(ProgrammableSwitchEvent);
        }

        public ProgrammableSwitchEventCharacteristic ProgrammableSwitchEvent { get; } = new();

        public NameCharacteristic? Name { get; private set; }
        public ServiceLabelIndexCharacteristic? ServiceLabelIndex { get; private set; }

        [MemberNotNull(nameof(Name))]
        public void AddName(string value)
        {
            Name ??= new();
            Name.Value = value;
            Characteristics.Add(Name);
        }

        [MemberNotNull(nameof(ServiceLabelIndex))]
        public void AddServiceLabelIndex(byte value)
        {
            ServiceLabelIndex ??= new();
            ServiceLabelIndex.Value = value;
            Characteristics.Add(ServiceLabelIndex);
        }
    }
}

