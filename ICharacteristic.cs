namespace HomeKit
{
    public interface ICharacteristic
    {
        public int Iid { get; }
        public string Type { get; }
        public string Format { get; }
        public string[] Perms { get; }
        public object Value { get; set; }
    }

    //public interface IAccessoryCharacteristic
    //{
    //    [JsonIgnore]
    //    public int Aid { get; }
    //}
}
