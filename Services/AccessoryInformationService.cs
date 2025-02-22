using System.Diagnostics.CodeAnalysis;
using HomeKit.Characteristics;

namespace HomeKit.Services
{
    public class AccessoryInformationService : Service
    {
        public AccessoryInformationService() : base("3E")
        {
            Characteristics.Add(Identify);
            Characteristics.Add(Manufacturer);
            Characteristics.Add(Model);
            Characteristics.Add(Name);
            Characteristics.Add(SerialNumber);
            Characteristics.Add(FirmwareRevision);
        }

        public IdentifyCharacteristic Identify { get; } = new();
        public ManufacturerCharacteristic Manufacturer { get; } = new();
        public ModelCharacteristic Model { get; } = new();
        public NameCharacteristic Name { get; } = new();
        public SerialNumberCharacteristic SerialNumber { get; } = new();
        public FirmwareRevisionCharacteristic FirmwareRevision { get; } = new();

        public HardwareRevisionCharacteristic? HardwareRevision { get; private set; }
        public AccessoryFlagsCharacteristic? AccessoryFlags { get; private set; }

        [MemberNotNull(nameof(HardwareRevision))]
        public void AddHardwareRevision(string value)
        {
            HardwareRevision ??= new();
            HardwareRevision.Value = value;
            Characteristics.Add(HardwareRevision);
        }

        [MemberNotNull(nameof(AccessoryFlags))]
        public void AddAccessoryFlags(uint value)
        {
            AccessoryFlags ??= new();
            AccessoryFlags.Value = value;
            Characteristics.Add(AccessoryFlags);
        }
    }
}

