using CUE4Parse.UE4.Assets.Objects;
using CUE4Parse.UE4.Assets.Utils;
using CUE4Parse.UE4.Objects.Core.Math;

namespace CUE4Parse.UE4.Assets.Exports.Component.SplineMesh;

[StructFallback]
public class FSplineMeshParams
{
    public FVector StartPos;
    public FVector StartTangent;
    public FVector2D StartOffset;
    public FVector EndPos;
    public FVector EndTangent;
    public FVector2D EndOffset;

    public FSplineMeshParams(FStructFallback fallback)
    {
        StartPos = fallback.GetOrDefault<FVector>(nameof(StartPos));
        StartTangent = fallback.GetOrDefault<FVector>(nameof(StartTangent));
        StartOffset = fallback.GetOrDefault<FVector2D>(nameof(StartOffset));
        EndPos = fallback.GetOrDefault<FVector>(nameof(EndPos));
        EndTangent = fallback.GetOrDefault<FVector>(nameof(EndTangent));
        EndOffset = fallback.GetOrDefault<FVector2D>(nameof(EndOffset));
    }
}
