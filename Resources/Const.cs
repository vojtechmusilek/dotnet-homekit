namespace HomeKit.Resources
{
    internal class Const
    {
        /// <summary>Accessory ID used when not bridged</summary>
        public const int StandaloneAid = 1;

        public const int MdnsDomainNamePointerType = 12;
        public const string MdnsHapDomainName = "_hap._tcp.local.";
        public const string MdnsLocal = ".local.";

        public const int FlagsQueryOrResponsePosition = 15;
        public const int FlagsAuthoritativeAnswerPosition = 10;
    }
}
