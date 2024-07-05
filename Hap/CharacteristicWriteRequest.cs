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
        public readonly object? Value { get; init; }
        public readonly bool? Ev { get; init; }
    }
}
