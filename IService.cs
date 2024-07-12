using System.Collections.Generic;

namespace HomeKit
{
    public interface IService
    {
        public int Iid { get; set; }
        public string Type { get; }
        public List<ICharacteristic> Characteristics { get; }
    }
}
