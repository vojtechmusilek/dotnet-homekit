# dotnet-homekit

### work in progress ...

- [x] mdns discovery 
- [x] hap pairing, publishing
- [x] state persistence
- [ ] accessory bridge
- [ ] value change events
- [ ] test accessories
- [ ] accessory classes generation

### current usage

```csharp
var switchAccessory = new Accessory("TestAcc", Category.Switch);

var switchService1 = switchAccessory.AddService(ServiceType.Switch);
switchService1.AddCharacteristic(CharacteristicType.Name)!.Value = "S1";
switchService1.GetCharacteristic(CharacteristicType.On)!.Value = true;

var switchService2 = switchAccessory.AddService(ServiceType.Switch);
switchService2.AddCharacteristic(CharacteristicType.Name)!.Value = "S2";
switchService2.GetCharacteristic(CharacteristicType.On)!.Value = false;

await switchAccessory.PublishAsync(new()
{
    IpAddress = "192.168.1.101",
    Port = 25252,
    PinCode = "123-00-123",
    MacAddress = "41:F8:F3:AC:16:36",
    StateDirectory = "c:/temp",
}, CancellationToken.None);

```