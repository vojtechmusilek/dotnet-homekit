using System;
using System.Collections.Generic;

namespace HomeKit.Resources
{
    internal record CharacteristicDef
    {
        public required CharacteristicType Type { get; init; }
        public required Guid Uuid { get; init; }
        public required string Format { get; init; }
        public required List<string> Permissions { get; init; }

        public Dictionary<string, int>? ValidValues { get; init; }
        public Dictionary<string, string>? ValidBits { get; init; }
        public double? MaxValue { get; init; }
        public double? MinValue { get; init; }
        public double? MinStep { get; init; }
        public string? Unit { get; init; }
    }
}
