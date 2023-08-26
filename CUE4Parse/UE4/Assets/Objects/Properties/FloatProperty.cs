using CUE4Parse.UE4.Readers;
using Newtonsoft.Json;

namespace CUE4Parse.UE4.Assets.Objects.Properties
{
    [JsonConverter(typeof(FloatPropertyConverter))]
    public class FloatProperty : FPropertyTagType<float>
    {
        public FloatProperty(FArchive Ar, ReadType type)
        {
            Value = type switch
            {
                ReadType.ZERO => 0,
                _ => Ar.Read<float>()
            };
        }
    }
}
