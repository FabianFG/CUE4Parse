using CUE4Parse.UE4.Readers;
using Newtonsoft.Json;

namespace CUE4Parse.UE4.Assets.Objects.Properties
{
    [JsonConverter(typeof(IntPropertyConverter))]
    public class IntProperty : FPropertyTagType<int>
    {
        public IntProperty(FArchive Ar, ReadType type)
        {
            Value = type switch
            {
                ReadType.ZERO => 0,
                _ => Ar.Read<int>()
            };
        }
    }
}
