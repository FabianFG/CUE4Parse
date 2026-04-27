using CUE4Parse.UE4.Objects.Core.Math;

namespace CUE4Parse_Conversion.V2.Dto;

public readonly struct MeshVertexColor(string name, FColor[] colors)
{
    public readonly string Name = name;
    public readonly FColor[] Colors = colors;
}
