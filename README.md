# dotnet-homekit

## Examples

Examples are available at [dotnet-homekit-examples](https://github.com/vojtechmusilek/dotnet-homekit-examples)

## How to install

Available on NuGet: [HomeKit.Server](https://www.nuget.org/packages/HomeKit.Server)

Install using command:

```sh
dotnet add package HomeKit.Server
```

or add package reference with latest version:

```xml
<PackageReference Include="HomeKit.Server" Version="0.0.0" />
```

## Typed HAP services

full list in [dotnet-homekit/Services](./Services)

```csharp
var fan = new FanService();

// always has On
fan.On.Value = true;

// optional characteristics
fan.AddRotationSpeed(50);
fan.RotationSpeed.Value = 45;

```
