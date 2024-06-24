using System.Collections.Generic;

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
        public IPacketRecordData Data;

        public ushort DataLength;
    }

    internal interface IPacketRecordData;

    internal record struct UnknownPacketRecordData : IPacketRecordData;

    internal record struct PacketRecordData_PTR : IPacketRecordData
    {
        public const int Type = 12;

        public string Name;
    }

    internal record struct PacketRecordData_SRV : IPacketRecordData
    {
        public const int Type = 33;

        public ushort Priority;
        public ushort Weight;
        public ushort Port;
        public string Target;
    }

    internal record struct PacketRecordData_TXT : IPacketRecordData
    {
        public const int Type = 16;

        public Dictionary<string, string> KeyValuePairs;
    }

    internal record struct PacketRecordData_A : IPacketRecordData
    {
        public const int Type = 1;

        public string IpAddress;
    }

    internal record struct PacketRecordData_NSEC : IPacketRecordData
    {
        public const int Type = 47;

        public string NextName;
        public HashSet<ushort> IncludedTypes;
    }
}
