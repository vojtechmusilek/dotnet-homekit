using System.Collections.Generic;

namespace HomeKit.Hap
{
    internal record struct Tlv
    {
        public byte Tag;
        public List<byte> Value;

        public byte Length;
    }
}
