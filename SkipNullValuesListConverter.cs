using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace HomeKit
{
    public class SkipNullValuesListConverter<T> : JsonConverter<List<T>>
    {
        public override List<T> Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            return JsonSerializer.Deserialize<List<T>>(ref reader, options)!;
        }

        public override void Write(Utf8JsonWriter writer, List<T> value, JsonSerializerOptions options)
        {
            writer.WriteStartArray();
            foreach (var item in value)
            {
                if (item != null)
                {
                    JsonSerializer.Serialize(writer, item, options);
                }
            }
            writer.WriteEndArray();
        }
    }
}
