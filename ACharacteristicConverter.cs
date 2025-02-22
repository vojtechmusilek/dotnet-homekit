using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using HomeKit.Characteristics;
using HomeKit.Characteristics.Abstract;

namespace HomeKit
{
    internal class ACharacteristicConverter : JsonConverter<ACharacteristic>
    {
        public override ACharacteristic Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            throw new NotImplementedException("Deserialization is not implemented");
        }

        public override void Write(Utf8JsonWriter writer, ACharacteristic value, JsonSerializerOptions options)
        {
            writer.WriteStartObject();
            writer.WriteNumber("iid", value.Iid);
            writer.WriteString("type", value.Type);
            writer.WriteString("format", value.Format);
            writer.WriteStartArray("perms");
            foreach (var perm in value.Perms)
            {
                writer.WriteStringValue(perm);
            }
            writer.WriteEndArray();

            if (value is BoolCharacteristic boolCharacteristic)
            {
                if (value is not IdentifyCharacteristics)
                {
                    writer.WriteBoolean("value", boolCharacteristic.Value);
                }
            }
            else if (value is FloatCharacteristic floatCharacteristic)
            {
                writer.WriteNumber("value", floatCharacteristic.Value);
            }
            else if (value is StringCharacteristic stringCharacteristic)
            {
                writer.WriteString("value", stringCharacteristic.Value);
            }
            else
            {
                throw new NotImplementedException("ACharacteristicConverter: " + value.GetType().FullName);
            }

            writer.WriteEndObject();
        }
    }
}
