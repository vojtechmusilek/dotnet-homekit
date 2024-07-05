namespace HomeKit.Hap
{
    internal readonly struct CharacteristicReadResponse
    {
        public readonly CharacteristicRead[] Characteristics { get; init; }
    }

    internal readonly struct CharacteristicRead
    {
        public readonly int Aid { get; init; }
        public readonly int Iid { get; init; }
        public readonly object? Value { get; init; }
        public readonly int? Status { get; init; }
    }
}
