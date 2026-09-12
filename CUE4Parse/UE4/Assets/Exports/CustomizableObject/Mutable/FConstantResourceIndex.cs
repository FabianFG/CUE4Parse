using Newtonsoft.Json;

namespace CUE4Parse.UE4.Assets.Exports.CustomizableObject.Mutable;

public struct FConstantResourceIndex
{
#pragma warning disable CS0649
    [JsonIgnore] private uint Packed;
#pragma warning restore CS0649
    
    public uint Index => Packed & 0x7FFFFFFF;
    public bool Streamable => Packed >> 31 != 0;
}
