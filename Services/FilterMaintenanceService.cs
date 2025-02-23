using System.Diagnostics.CodeAnalysis;
using HomeKit.Characteristics;

namespace HomeKit.Services
{
    public class FilterMaintenanceService : Service
    {
        public FilterMaintenanceService() : base("BA")
        {
            Characteristics.Add(FilterChangeIndication);
        }

        public FilterChangeIndicationCharacteristic FilterChangeIndication { get; } = new();

        public FilterLifeLevelCharacteristic? FilterLifeLevel { get; private set; }
        public ResetFilterIndicationCharacteristic? ResetFilterIndication { get; private set; }
        public NameCharacteristic? Name { get; private set; }

        [MemberNotNull(nameof(FilterLifeLevel))]
        public void AddFilterLifeLevel(float value)
        {
            FilterLifeLevel ??= new();
            FilterLifeLevel.Value = value;
            Characteristics.Add(FilterLifeLevel);
        }

        [MemberNotNull(nameof(ResetFilterIndication))]
        public void AddResetFilterIndication(byte value)
        {
            ResetFilterIndication ??= new();
            ResetFilterIndication.Value = value;
            Characteristics.Add(ResetFilterIndication);
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

