# dotnet-homekit

### work in progress ...

- [x] mdns discovery 
- [x] hap pairing, publishing
- [x] state persistence
- [x] accessory bridge
- [x] value change events
- [ ] test accessories
- [ ] accessory classes generation

### current usage

#### bridge accessory with 2 switches

```csharp
var bridge = new AccessoryBridge("TestBridge", m_LoggerFactory);

var switch1 = new Accessory("TestSwitch1", Category.Switch, m_LoggerFactory);
var switch1_service = switch1.AddService(ServiceType.Switch);
switch1_service.AddCharacteristic(CharacteristicType.Name)!.Value = "S1";
switch1_service.GetCharacteristic(CharacteristicType.On)!.Value = true;

var switch2 = new Accessory("TestSwitch2", Category.Switch, m_LoggerFactory);
var switch2_service = switch2.AddService(ServiceType.Switch);
switch2_service.AddCharacteristic(CharacteristicType.Name)!.Value = "S2";
switch2_service.GetCharacteristic(CharacteristicType.On)!.Value = false;

bridge.Accessories.Add(switch1);
bridge.Accessories.Add(switch2);
```

#### single accessory switch

```csharp
var accessory = new Accessory("TestAcc", Category.Switch);

var service1 = accessory.AddService(ServiceType.Switch);
service1.AddCharacteristic(CharacteristicType.Name)!.Value = "S1";
service1.GetCharacteristic(CharacteristicType.On)!.Value = true;

var service2 = accessory.AddService(ServiceType.Switch);
service2.AddCharacteristic(CharacteristicType.Name)!.Value = "S2";
service2.GetCharacteristic(CharacteristicType.On)!.Value = false;
```

#### publishing accessory/bridge

```csharp
await accessory.PublishAsync(new()
{
    IpAddress = "192.168.1.101",
    Port = 25252,
    PinCode = "123-00-123",
    MacAddress = "41:F8:F3:AC:16:36",
    StateDirectory = "c:/temp",
}, CancellationToken.None);

```