namespace HomeKit.Resources
{
    internal class Const
    {
        /// <summary>Accessory ID used when not bridged</summary>
        public const int StandaloneAid = 1;

        public const string MdnsHapDomainName = "_hap._tcp.local.";
        public const string MdnsLocal = "local.";

        public const int MdnsType_SOA = 6;
        public const int MdnsType_NSAPPTR = 23;
        public const int MdnsType_A6 = 38;

        public const int FlagsQueryOrResponsePosition = 15;
        public const int FlagsAuthoritativeAnswerPosition = 10;

        public const uint LongTtl = 4500;
        public const uint ShortTtl = 120;
    }
}
