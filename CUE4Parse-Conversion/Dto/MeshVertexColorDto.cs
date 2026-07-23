using CUE4Parse.UE4.Objects.Core.Math;

namespace CUE4Parse_Conversion.Dto;

public readonly struct MeshVertexColorDto(string name, FColor[] colors)
{
    public readonly string Name = name;
    public readonly FColor[] Colors = colors;
}
