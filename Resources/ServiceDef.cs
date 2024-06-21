using System;
using System.Collections.Generic;

namespace HomeKit.Resources
{
    public record ServiceDef
    {
        public required ServiceType Type { get; init; }
        public required Guid Uuid { get; init; }
        public required List<string> OptionalCharacteristics { get; init; }
        public required List<string> RequiredCharacteristics { get; init; }
    }
}
