using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using HomeKit.Resources;
using HomeKit.Services;

namespace HomeKit
{
    public class Accessory
    {
        private readonly string m_Name;
        private readonly Category m_Category;

        public int Aid { get; set; }
        public List<Service> Services { get; } = new();

        public Accessory(string name, Category category = Category.Other)
        {
            m_Name = name;
            m_Category = category;

            AddAccessoryInformationService();
        }

        protected virtual AccessoryServer PrepareServer(AccessoryServerOptions options)
        {
            var server = new AccessoryServer(options);
            server.Accessories.Add(this);
            return server;
        }

        public async Task<AccessoryServer> PublishAsync(AccessoryServerOptions options, CancellationToken? cancellationToken = null)
        {
            options.Name ??= m_Name;
            options.Category ??= m_Category;

            var server = PrepareServer(options);
            await server.StartAsync(cancellationToken ?? CancellationToken.None);

            return server;
        }

        private void AddAccessoryInformationService()
        {
            var info = new AccessoryInformationService();

            info.Name.Value = m_Name;
            info.SerialNumber.Value = m_Name + " SerialNumber";
            info.Manufacturer.Value = m_Name + " Manufacturer";
            info.Model.Value = m_Name + " Model";
            info.FirmwareRevision.Value = "1.0";

            Services.Add(info);
        }
    }
}
