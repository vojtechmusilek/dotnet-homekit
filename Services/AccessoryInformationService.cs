using System.Text.Json.Serialization;
using HomeKit.Characteristics;

namespace HomeKit.Services
{
    public class AccessoryInformationService : AService
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

        [JsonIgnore] public IdentifyCharacteristics Identify { get; } = new();
        [JsonIgnore] public ManufacturerCharacteristics Manufacturer { get; } = new();
        [JsonIgnore] public ModelCharacteristics Model { get; } = new();
        [JsonIgnore] public NameCharacteristic Name { get; } = new();
        [JsonIgnore] public SerialNumberCharacteristics SerialNumber { get; } = new();
        [JsonIgnore] public FirmwareRevisionCharacteristics FirmwareRevision { get; } = new();
    }
}
