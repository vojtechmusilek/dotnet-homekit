using System.Diagnostics.CodeAnalysis;
using HomeKit.Characteristics;

namespace HomeKit.Services
{
    public class ServiceLabelService : Service
    {
        public ServiceLabelService() : base("CC")
        {
            Characteristics.Add(ServiceLabelNamespace);
        }

        public ServiceLabelNamespaceCharacteristic ServiceLabelNamespace { get; } = new();

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

