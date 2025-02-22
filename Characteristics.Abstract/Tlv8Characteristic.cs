using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeKit.Characteristics.Abstract
{
    public abstract class Tlv8Characteristic : ACharacteristic
    {
        public override string Format => "tlv8";
    }
}
