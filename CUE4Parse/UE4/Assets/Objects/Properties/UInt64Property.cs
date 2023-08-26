using CUE4Parse.UE4.Readers;
using Newtonsoft.Json;

namespace CUE4Parse.UE4.Assets.Objects.Properties
{
    [JsonConverter(typeof(UInt64PropertyConverter))]
    public class UInt64Property : FPropertyTagType<ulong>
    {
        public UInt64Property(FArchive Ar, ReadType type)
        {
            Value = type switch
            {
                ReadType.ZERO => 0,
                _ => Ar.Read<ulong>()
            };
        }
    }
}
