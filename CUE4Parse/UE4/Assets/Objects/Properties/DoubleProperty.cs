using CUE4Parse.UE4.Readers;
using Newtonsoft.Json;

namespace CUE4Parse.UE4.Assets.Objects.Properties
{
    [JsonConverter(typeof(DoublePropertyConverter))]
    public class DoubleProperty : FPropertyTagType<double>
    {
        public DoubleProperty(FArchive Ar, ReadType type)
        {
            Value = type switch
            {
                ReadType.ZERO => 0.0,
                _ => Ar.Read<double>()
            };
        }
    }
}
