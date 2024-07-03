namespace HomeKit.Resources
{
    //[JsonConverter(typeof(JsonStringEnumConverter))]
    //internal enum CharacteristicPermission
    //{
    //    [JsonPropertyName("pr")] PairedRead,
    //    [JsonPropertyName("pw")] PairedWrite,
    //    [JsonPropertyName("ev")] Events,
    //    [JsonPropertyName("aa")] AdditionalAuthorization,
    //    [JsonPropertyName("tw")] TimedWrite,
    //    [JsonPropertyName("hd")] Hidden,
    //    [JsonPropertyName("wr")] WriteResponse,
    //}

    public static class CharacteristicPermission
    {
        public const string PairedRead = "pr";
        public const string PairedWrite = "pw";
        public const string Events = "ev";
        public const string AdditionalAuthorization = "aa";
        public const string TimedWrite = "tw";
        public const string Hidden = "hd";
        public const string WriteResponse = "wr";
    }
}
