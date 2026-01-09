using Newtonsoft.Json;

namespace CUE4Parse.UE4.Assets.Exports.CustomizableObject.Mutable;

public struct FConstantResourceIndex
{
    [JsonIgnore] private uint Packed;
    
    public uint Index => Packed & 0x7FFFFFFF;
    public bool Streamable => Packed >> 31 != 0;
}
