using CUE4Parse.UE4.Assets.Objects;
using CUE4Parse.UE4.Assets.Utils;

namespace CUE4Parse.UE4.Objects.Core.Math;

[StructFallback]
public readonly struct FIntMargin : IUStruct
{
    public readonly int Left;
    public readonly int Top;
    public readonly int Right;
    public readonly int Bottom;

    public FIntMargin(int margin) => Left = Top = Right = Bottom = margin;

    public FIntMargin(FStructFallback fallback)
    {
        Left = fallback.GetOrDefault<int>(nameof(Left));
        Top = fallback.GetOrDefault<int>(nameof(Top));
        Right = fallback.GetOrDefault<int>(nameof(Right));
        Bottom = fallback.GetOrDefault<int>(nameof(Bottom));
    }
}
