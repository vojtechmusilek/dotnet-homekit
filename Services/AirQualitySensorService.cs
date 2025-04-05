using System.Diagnostics.CodeAnalysis;
using HomeKit.Characteristics;

namespace HomeKit.Services
{
    public class AirQualitySensorService : Service
    {
        public AirQualitySensorService() : base("8D")
        {
            Characteristics.Add(AirQuality);
        }

        public AirQualityCharacteristic AirQuality { get; } = new();

        public StatusActiveCharacteristic? StatusActive { get; private set; }
        public StatusFaultCharacteristic? StatusFault { get; private set; }
        public StatusTamperedCharacteristic? StatusTampered { get; private set; }
        public StatusLowBatteryCharacteristic? StatusLowBattery { get; private set; }
        public NameCharacteristic? Name { get; private set; }
        public OzoneDensityCharacteristic? OzoneDensity { get; private set; }
        public NitrogenDioxideDensityCharacteristic? NitrogenDioxideDensity { get; private set; }
        public SulphurDioxideDensityCharacteristic? SulphurDioxideDensity { get; private set; }
        public PM2DensityCharacteristic? PM2Density { get; private set; }
        public PM10DensityCharacteristic? PM10Density { get; private set; }
        public VOCDensityCharacteristic? VOCDensity { get; private set; }
        public CarbonMonoxideLevelCharacteristic? CarbonMonoxideLevel { get; private set; }
        public CarbonDioxideLevelCharacteristic? CarbonDioxideLevel { get; private set; }

        [MemberNotNull(nameof(StatusActive))]
        public void AddStatusActive(bool value)
        {
            StatusActive ??= new();
            StatusActive.Value = value;
            Characteristics.Add(StatusActive);
        }

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

        [MemberNotNull(nameof(StatusLowBattery))]
        public void AddStatusLowBattery(byte value)
        {
            StatusLowBattery ??= new();
            StatusLowBattery.Value = value;
            Characteristics.Add(StatusLowBattery);
        }

        [MemberNotNull(nameof(Name))]
        public void AddName(string value)
        {
            Name ??= new();
            Name.Value = value;
            Characteristics.Add(Name);
        }

        [MemberNotNull(nameof(OzoneDensity))]
        public void AddOzoneDensity(float value)
        {
            OzoneDensity ??= new();
            OzoneDensity.Value = value;
            Characteristics.Add(OzoneDensity);
        }

        [MemberNotNull(nameof(NitrogenDioxideDensity))]
        public void AddNitrogenDioxideDensity(float value)
        {
            NitrogenDioxideDensity ??= new();
            NitrogenDioxideDensity.Value = value;
            Characteristics.Add(NitrogenDioxideDensity);
        }

        [MemberNotNull(nameof(SulphurDioxideDensity))]
        public void AddSulphurDioxideDensity(float value)
        {
            SulphurDioxideDensity ??= new();
            SulphurDioxideDensity.Value = value;
            Characteristics.Add(SulphurDioxideDensity);
        }

        [MemberNotNull(nameof(PM2Density))]
        public void AddPM2Density(float value)
        {
            PM2Density ??= new();
            PM2Density.Value = value;
            Characteristics.Add(PM2Density);
        }

        [MemberNotNull(nameof(PM10Density))]
        public void AddPM10Density(float value)
        {
            PM10Density ??= new();
            PM10Density.Value = value;
            Characteristics.Add(PM10Density);
        }

        [MemberNotNull(nameof(VOCDensity))]
        public void AddVOCDensity(float value)
        {
            VOCDensity ??= new();
            VOCDensity.Value = value;
            Characteristics.Add(VOCDensity);
        }

        [MemberNotNull(nameof(CarbonMonoxideLevel))]
        public void AddCarbonMonoxideLevel(float value)
        {
            CarbonMonoxideLevel ??= new();
            CarbonMonoxideLevel.Value = value;
            Characteristics.Add(CarbonMonoxideLevel);
        }

        [MemberNotNull(nameof(CarbonDioxideLevel))]
        public void AddCarbonDioxideLevel(float value)
        {
            CarbonDioxideLevel ??= new();
            CarbonDioxideLevel.Value = value;
            Characteristics.Add(CarbonDioxideLevel);
        }
    }
}

