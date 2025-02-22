using System.Diagnostics.CodeAnalysis;
using HomeKit.Characteristics;

namespace HomeKit.Services
{
    public class FaucetService : Service
    {
        public FaucetService() : base("D7")
        {
            Characteristics.Add(Active);
        }

        public ActiveCharacteristic Active { get; } = new();

        public NameCharacteristic? Name { get; private set; }
        public StatusFaultCharacteristic? StatusFault { get; private set; }

        [MemberNotNull(nameof(Name))]
        public void AddName(string value)
        {
            Name ??= new();
            Name.Value = value;
            Characteristics.Add(Name);
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

