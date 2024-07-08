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
        private readonly object? m_Value;

        public readonly int Aid { get; init; }
        public readonly int Iid { get; init; }
        public readonly object? Value { get => ValueGetter(); init => m_Value = value; }
        public readonly bool? Ev { get; init; }

        private object? ValueGetter()
        {
            if (m_Value is null)
            {
                return null;
            }

            if (m_Value is JsonElement jsonElement)
            {
                return jsonElement.ValueKind switch
                {
                    JsonValueKind.String => jsonElement.GetString(),
                    JsonValueKind.Number => jsonElement.GetDouble(),
                    JsonValueKind.True => true,
                    JsonValueKind.False => false,
                    _ => null,
                };
            }

            return m_Value;
        }
    }
}
