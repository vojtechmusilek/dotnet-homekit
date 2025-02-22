using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeKit.Characteristics.Abstract
{
    public abstract class Uint32Characteristic : ACharacteristic
    {
        public override string Format => "uint32";
    }
}
