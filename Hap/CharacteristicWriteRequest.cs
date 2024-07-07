using System.Text.Json;

namespace HomeKit.Hap
{
    /// 6.7.2
    internal readonly struct CharacteristicWriteRequest
    {
        public readonly CharacteristicWrite[] Characteristics { get; init; }
    }

    /// 6.7.2.1
    internal readonly struct CharacteristicWrite
    {
        public readonly int Aid { get; init; }
        public readonly int Iid { get; init; }
        public readonly JsonElement? Value { private get; init; }
        public readonly bool? Ev { get; init; }

        public object? GetParsedValue()
        {
            if (Value is null)
            {
                return null;
            }

            return Value.Value.ValueKind switch
            {
                JsonValueKind.String => Value.Value.GetString(),
                JsonValueKind.Number => Value.Value.GetDouble(),
                JsonValueKind.True => true,
                JsonValueKind.False => false,
                _ => null,
            };
        }
    }
}
