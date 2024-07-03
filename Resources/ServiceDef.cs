using System;
using System.Collections.Generic;

namespace HomeKit.Resources
{
    public record ServiceDef
    {
        private const string m_BaseUuid = "0000-1000-8000-0026BB765291";

        public required ServiceType Type { get; init; }
        public required Guid Uuid { get; init; }
        public required List<string> OptionalCharacteristics { get; init; }
        public required List<string> RequiredCharacteristics { get; init; }

        public string GetHapType()
        {
            var uppercaseUuid = Uuid.ToString().ToUpper();

            if (uppercaseUuid.EndsWith(m_BaseUuid))
            {
                return uppercaseUuid.Split('-')[0].TrimStart('0');
            }

            return uppercaseUuid;
        }
    }
}
