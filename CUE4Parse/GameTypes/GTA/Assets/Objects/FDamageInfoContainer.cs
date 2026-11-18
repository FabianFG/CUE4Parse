using CUE4Parse.UE4;
using CUE4Parse.UE4.Assets.Objects;
using CUE4Parse.UE4.Assets.Utils;

namespace CUE4Parse.GameTypes.GTA.Assets.Objects;

[StructFallback]
public readonly struct FDamageInfoContainer : IUStruct
{
    public readonly int[]? NumTrianglesOk;
    public readonly int[]? NumTriangles;

    public FDamageInfoContainer(FStructFallback fallback)
    {
        NumTrianglesOk = fallback.GetOrDefault<int[]?>(nameof(NumTrianglesOk));
        NumTriangles = fallback.GetOrDefault<int[]?>(nameof(NumTriangles));
    }
}
