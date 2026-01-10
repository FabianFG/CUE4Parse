using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace CUE4Parse.UE4.Objects.Engine.EdGraph;

[JsonConverter(typeof(StringEnumConverter))]
public enum EPinContainerType : byte
{
    None,
    Array,
    Set,
    Map
};
