# dotnet-homekit

### accessory with 2 switches

```csharp
// create accessory
var accessory = new Accessory("Switches", Category.Switch);

// add 2 switches using AddService
var switch1 = accessory.AddService<SwitchService>();
var switch2 = accessory.AddService<SwitchService>();

// or add existing Service directly to Services list
// accessory.Services.Add(someService);

// log value on change
switch1.On.Changed += (sender, newValue) => Console.WriteLine($"switch1: {newValue}");
switch2.On.Changed += (sender, newValue) => Console.WriteLine($"switch2: {newValue}");

await accessory.PublishAsync(new AccessoryServerOptions()
{
    // set mac persistent address
    MacAddress = "11:22:33:00:00:00",
});

while (true)
{
    await Task.Delay(3000);

    // mirror switch value
    switch1.On.Value = switch2.On.Value;
}
```

### accessory bridge

```csharp
// create bridge
var bridge = new AccessoryBridge("Bridge");

// create accessories
var accessory1 = new Accessory("Switch 1");
accessory1.AddService<SwitchService>();

var accessory2 = new Accessory("Switch 2");
accessory2.AddService<SwitchService>();

// add to bridge
bridge.Accessories.Add(accessory1);
bridge.Accessories.Add(accessory2);
```

### typed services

full list in [Services](./Services)

```csharp
var fan = new FanService();

// always has On
fan.On.Value = true;

// optional characteristics
fan.AddRotationSpeed(50);

fan.RotationSpeed.Value = 45;

```