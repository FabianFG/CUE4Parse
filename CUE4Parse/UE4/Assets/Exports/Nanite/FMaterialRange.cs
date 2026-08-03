namespace CUE4Parse.UE4.Assets.Exports.Nanite;

public readonly struct FMaterialRange(uint triStart, uint triLength, uint materialIndex)
{
    /// <summary>The index of the first triangle that uses this material.</summary>
    public readonly uint TriStart = triStart;
    /// <summary>The number of triangles that use this material.</summary>
    public readonly uint TriLength = triLength;
    /// <summary>The index of the material used by the triangles this range points to.</summary>
    public readonly uint MaterialIndex = materialIndex;

    public FMaterialRange(uint data) : this(
        NaniteUtils.GetBits(data, 8, 0),
        NaniteUtils.GetBits(data, 8, 8),
        NaniteUtils.GetBits(data, 6, 16)
        )
    {

    }

    public FMaterialRange(FMaterialRange range, uint materialIndex) : this(range.TriStart, range.TriLength, materialIndex)
    {

    }
}
