using System.Collections.Generic;
using System.Linq;
using HomeKit.Resources;

namespace HomeKit
{
    public class Service : IService
    {
        private readonly ServiceDef m_Def;

        public int Iid { get; set; }
        public string Type { get; }
        public List<ICharacteristic> Characteristics { get; }

        public Service(ServiceType serviceType)
        {
            m_Def = ServiceDefs.Get(serviceType);

            Type = Utils.GetHapType(m_Def.Uuid);
            Characteristics = new();

            foreach (var type in m_Def.RequiredCharacteristics)
            {
                Characteristics.Add(new Characteristic(type));
            }
        }

        public Characteristic? AddCharacteristic(CharacteristicType type)
        {
            if (!m_Def.OptionalCharacteristics.Contains(type))
            {
                return null;
            }

            var characteristic = new Characteristic(type);
            Characteristics.Add(characteristic);
            return characteristic;
        }

        public Characteristic? GetCharacteristic(CharacteristicType type)
        {
            return Characteristics.OfType<Characteristic>().FirstOrDefault(x => x.IsType(type));
        }
    }
}
