namespace HomeKit.Hap
{
    internal enum TlvType : byte
    {
        Method = 0x00,
        Identifier = 0x01,
        Salt = 0x02,
        PublicKey = 0x03,
        Proof = 0x04,
        EncryptedData = 0x05,
        State = 0x06,
        Error = 0x07,
        RetryDelay = 0x08,
        Certificate = 0x09,
        Signature = 0x0A,
        Permissions = 0x0B,
        FragmentData = 0x0C,
        FragmentLast = 0x0D,
        Flags = 0x13,
    }

    internal enum TlvError : byte
    {
        Unknown = 0x01,
        Authentication = 0x02,
        Backoff = 0x03,
        MaxPeers = 0x04,
        MaxTries = 0x05,
        Unavailable = 0x06,
        Busy = 0x07,
    }
}
