﻿using System.Text.Json.Serialization;

namespace HomeKit.Resources
{
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum ServiceType
    {
        None,
        AccessoryInformation,
        AirPurifier,
        AirQualitySensor,
        BatteryService,
        CameraRTPStreamManagement,
        CarbonDioxideSensor,
        CarbonMonoxideSensor,
        ContactSensor,
        Door,
        Doorbell,
        Fan,
        Fanv2,
        Faucet,
        FilterMaintenance,
        GarageDoorOpener,
        HeaterCooler,
        HumidifierDehumidifier,
        HumiditySensor,
        InputSource,
        IrrigationSystem,
        LeakSensor,
        LightSensor,
        Lightbulb,
        LockManagement,
        LockMechanism,
        Microphone,
        MotionSensor,
        OccupancySensor,
        Outlet,
        SecuritySystem,
        ServiceLabel,
        Slat,
        SmokeSensor,
        Speaker,
        StatelessProgrammableSwitch,
        Switch,
        Television,
        TelevisionSpeaker,
        TemperatureSensor,
        Thermostat,
        Valve,
        Window,
        WindowCovering,
    }
}