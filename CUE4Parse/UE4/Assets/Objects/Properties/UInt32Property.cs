using CUE4Parse.UE4.Readers;
using Newtonsoft.Json;

namespace CUE4Parse.UE4.Assets.Objects.Properties
{
    [JsonConverter(typeof(UInt32PropertyConverter))]
    public class UInt32Property : FPropertyTagType<uint>
    {
        public UInt32Property(FArchive Ar, ReadType type)
        {
            Value = type switch
            {
                ReadType.ZERO => 0,
                _ => Ar.Read<uint>()
            };
        }
    }
}
