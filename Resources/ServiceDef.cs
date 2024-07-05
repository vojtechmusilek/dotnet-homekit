using System;
using System.Collections.Generic;

namespace HomeKit.Resources
{
    public record ServiceDef
    {
        public required ServiceType Type { get; init; }
        public required Guid Uuid { get; init; }
        public required List<CharacteristicType> OptionalCharacteristics { get; init; }
        public required List<CharacteristicType> RequiredCharacteristics { get; init; }
    }
}
