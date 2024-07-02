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
        Signature = 0x0A,
    }

    internal enum TlvError : byte
    {
        Authentication = 0x02,
    }
}
