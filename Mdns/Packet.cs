using System.Collections.Generic;

namespace HomeKit.Mdns
{
    internal record struct Packet
    {
        public PacketHeader Header;
        public List<PacketQuestion> Questions;
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
}
