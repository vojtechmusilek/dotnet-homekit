using System.Diagnostics.CodeAnalysis;
using HomeKit.Characteristics;

namespace HomeKit.Services
{
    public class ThermostatService : Service
    {
        public ThermostatService() : base("4A")
        {
            Characteristics.Add(CurrentHeatingCoolingState);
            Characteristics.Add(TargetHeatingCoolingState);
            Characteristics.Add(CurrentTemperature);
            Characteristics.Add(TargetTemperature);
            Characteristics.Add(TemperatureDisplayUnits);
        }

        public CurrentHeatingCoolingStateCharacteristic CurrentHeatingCoolingState { get; } = new();
        public TargetHeatingCoolingStateCharacteristic TargetHeatingCoolingState { get; } = new();
        public CurrentTemperatureCharacteristic CurrentTemperature { get; } = new();
        public TargetTemperatureCharacteristic TargetTemperature { get; } = new();
        public TemperatureDisplayUnitsCharacteristic TemperatureDisplayUnits { get; } = new();

        public CurrentRelativeHumidityCharacteristic? CurrentRelativeHumidity { get; private set; }
        public TargetRelativeHumidityCharacteristic? TargetRelativeHumidity { get; private set; }
        public CoolingThresholdTemperatureCharacteristic? CoolingThresholdTemperature { get; private set; }
        public HeatingThresholdTemperatureCharacteristic? HeatingThresholdTemperature { get; private set; }
        public NameCharacteristic? Name { get; private set; }

        [MemberNotNull(nameof(CurrentRelativeHumidity))]
        public void AddCurrentRelativeHumidity(float value)
        {
            CurrentRelativeHumidity ??= new();
            CurrentRelativeHumidity.Value = value;
            Characteristics.Add(CurrentRelativeHumidity);
        }

        [MemberNotNull(nameof(TargetRelativeHumidity))]
        public void AddTargetRelativeHumidity(float value)
        {
            TargetRelativeHumidity ??= new();
            TargetRelativeHumidity.Value = value;
            Characteristics.Add(TargetRelativeHumidity);
        }

        [MemberNotNull(nameof(CoolingThresholdTemperature))]
        public void AddCoolingThresholdTemperature(float value)
        {
            CoolingThresholdTemperature ??= new();
            CoolingThresholdTemperature.Value = value;
            Characteristics.Add(CoolingThresholdTemperature);
        }

        [MemberNotNull(nameof(HeatingThresholdTemperature))]
        public void AddHeatingThresholdTemperature(float value)
        {
            HeatingThresholdTemperature ??= new();
            HeatingThresholdTemperature.Value = value;
            Characteristics.Add(HeatingThresholdTemperature);
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

