using HomeKit.Resources;
using Microsoft.Extensions.Logging;

namespace HomeKit
{
    public struct AccessoryServerOptions
    {
        public string? Name;
        public Category? Category;
        public string? IpAddress;
        public ushort? Port;
        public string? PinCode;
        public string? MacAddress;
        public string? StateDirectory;
        public ILoggerFactory? LoggerFactory;
    }
}
