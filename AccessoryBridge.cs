using System.Collections.Generic;
using HomeKit.Resources;

namespace HomeKit
{
    public class AccessoryBridge(string name) : Accessory(name, Category.Bridge)
    {
        public HashSet<Accessory> Accessories { get; } = new();

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
