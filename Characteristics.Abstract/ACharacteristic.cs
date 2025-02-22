using System.Text.Json.Serialization;

namespace HomeKit.Characteristics.Abstract
{
    //[JsonPolymorphic]
    //[JsonDerivedType(typeof(NameCharacteristic), typeDiscriminator: "nameCharacteristic")]
    public abstract class ACharacteristic
    {
        [JsonIgnore]
        public int Aid { get; set; }
        public int Iid { get; set; }
        public abstract string Type { get; }
        public abstract string Format { get; }
        public abstract string[] Perms { get; }
        //public object Value { get; set; }
    }

    //public abstract class ACharacteristic<T> : ACharacteristic where T : struct
    //{

    //    [JsonIgnore]
    //    public new T TValue { get => (T)Value; set => Value = value; }
    //}

    //public interface IAccessoryCharacteristic
    //{
    //    [JsonIgnore]
    //    public int Aid { get; }
    //}
}
