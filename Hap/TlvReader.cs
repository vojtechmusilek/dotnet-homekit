using System;
using System.Collections.Generic;
using System.Linq;

namespace HomeKit.Hap
{
    internal static class TlvReader
    {
        public static Tlv[] ReadTlvs(ReadOnlySpan<byte> data)
        {
            var tlvs = new List<Tlv>();
            var position = 0;

            while (position < data.Length)
            {
                var tag = data[position];
                var length = data[position + 1];
                var value = data[(position + 2)..(position + 2 + length)];

                var target = tlvs.FirstOrDefault(it => it.Tag == tag);
                if (target != default)
                {
                    // todo does this happen?
                    target.Value.AddRange(value);
                }
                else
                {
                    tlvs.Add(new Tlv()
                    {
                        Tag = tag,
                        Value = new(value.ToArray()),
                        Length = length
                    });
                }

                position = position + 2 + length;
            }

            return tlvs.ToArray();
        }
    }
}
