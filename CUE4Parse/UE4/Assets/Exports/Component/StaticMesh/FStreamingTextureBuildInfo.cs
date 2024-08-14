using System.Runtime.InteropServices;

namespace CUE4Parse.UE4.Assets.Exports.Component.StaticMesh;

[StructLayout(LayoutKind.Sequential)]
public readonly struct FStreamingTextureBuildInfo
{
    public readonly uint PackedRelativeBox;
    public readonly int TextureLevelIndex;
    public readonly float TexelFactor;
}
