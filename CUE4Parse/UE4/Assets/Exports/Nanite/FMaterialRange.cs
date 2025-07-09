namespace CUE4Parse.UE4.Assets.Exports.Nanite;

public readonly struct FMaterialRange
{
    /// <summary>The index of the first triangle that uses this material.</summary>
    public readonly uint TriStart;
    /// <summary>The number of tirangles that use this material.</summary>
    public readonly uint TriLength;
    /// <summary>The index of the material used by the triangles this range points to.</summary>
    public readonly uint MaterialIndex;

    public FMaterialRange(uint data)
    {
        TriStart = NaniteUtils.GetBits(data, 8, 0);
        TriLength = NaniteUtils.GetBits(data, 8, 8);
        MaterialIndex = NaniteUtils.GetBits(data, 6, 16);
    }

    public FMaterialRange(uint triStart, uint triLength, uint materialIndex) {
        TriStart = triStart;
        TriLength = triLength;
        MaterialIndex = materialIndex;
    }
}
