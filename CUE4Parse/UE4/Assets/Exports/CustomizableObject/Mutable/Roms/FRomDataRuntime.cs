using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace CUE4Parse.UE4.Assets.Exports.CustomizableObject.Mutable.Roms;

public struct FRomDataRuntime
{
    [JsonIgnore] public uint Packed;
    
    public uint Size => Packed & 0x3FFFFFFF;
    public ERomDataType Type => (ERomDataType)((Packed >> 30) & 1);
    public bool IsHighRes => Packed >> 31 != 0;
}

[JsonConverter(typeof(StringEnumConverter))]
public enum ERomDataType : uint
{
    Image = 0,
    Mesh = 1
}
