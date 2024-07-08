using System.Collections.Generic;
using HomeKit.Resources;
using Microsoft.Extensions.Logging;

namespace HomeKit
{
    public class AccessoryBridge : Accessory
    {
        public HashSet<Accessory> Accessories { get; } = new();

        public AccessoryBridge(string name, ILoggerFactory? loggerFactory)
        : base(name, Category.Bridge, loggerFactory)
        {
        }

        protected override AccessoryServer PrepareServer(AccessoryServerOptions options)
        {
            var server = base.PrepareServer(options);

            foreach (var accessory in Accessories)
            {
                server.Accessories.Add(accessory);
            }

            return server;
        }
    }
}
