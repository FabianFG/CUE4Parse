using CUE4Parse.UE4.Readers;
using Newtonsoft.Json;

namespace CUE4Parse.UE4.Assets.Objects.Properties
{
    [JsonConverter(typeof(UInt16PropertyConverter))]
    public class UInt16Property : FPropertyTagType<ushort>
    {
        public UInt16Property(FArchive Ar, ReadType type)
        {
            Value = type switch
            {
                ReadType.ZERO => 0,
                _ => Ar.Read<ushort>()
            };
        }
    }
}
