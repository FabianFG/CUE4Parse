using CUE4Parse.UE4.Readers;
using Newtonsoft.Json;

namespace CUE4Parse.UE4.Assets.Objects.Properties
{
    [JsonConverter(typeof(Int64PropertyConverter))]
    public class Int64Property : FPropertyTagType<long>
    {
        public Int64Property(FArchive Ar, ReadType type)
        {
            Value = type switch
            {
                ReadType.ZERO => 0,
                _ => Ar.Read<long>()
            };
        }
    }
}
