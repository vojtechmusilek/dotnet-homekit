namespace HomeKit.Mdns
{
    internal record struct Packet
    {
        public PacketHeader Header;
        public PacketQuestion[] Questions;
        public PacketRecord[] Answers;
        public PacketRecord[] Authorities;
        public PacketRecord[] Additionals;
    }

    internal record struct PacketHeader
    {
        public ushort Id;
        public ushort Flags;
        public ushort Questions;
        public ushort Answers;
        public ushort Authorities;
        public ushort Additionals;
    }

    internal record struct PacketQuestion
    {
        public string Name;
        public ushort Type;
        public ushort Class;
    }

    internal record struct PacketRecord
    {
        public string Name;
        public ushort Type;
        public ushort Class;
        public uint Ttl;
        public ushort DataLength;
        public IPacketRecordData Data;
    }

    internal interface IPacketRecordData;

    internal record struct UnknownPacketRecordData : IPacketRecordData;

    internal record struct PtrPacketRecordData : IPacketRecordData
    {
        public string Name;
    }


}
