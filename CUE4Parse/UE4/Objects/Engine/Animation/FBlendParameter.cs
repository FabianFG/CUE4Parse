using CUE4Parse.UE4.Assets.Objects;
using CUE4Parse.UE4.Assets.Utils;

namespace CUE4Parse.UE4.Objects.Engine.Animation;

[StructFallback]
public readonly struct FBlendParameter
{
    public readonly string DisplayName;

    /// <summary>
    /// Minimum value for this axis range.
    /// </summary>
    public readonly float Min;

    /// <summary>
    /// Maximum value for this axis range.
    /// </summary>
    public readonly float Max;

    /// <summary>
    /// The number of grid divisions along this axis.
    /// </summary>
    public readonly int GridNum;

    /// <summary>
    /// If true then samples will always be snapped to the grid on this axis when added, moved, or the axes are changed.
    /// </summary>
    public readonly bool bSnapToGrid;

    /// <summary>
    /// If true then the input can go outside the min/max range and the blend space is treated as being cyclic on this axis.
    /// If false then input parameters are clamped to the min/max values on this axis.
    /// </summary>
    public readonly bool bWrapInput;

    public FBlendParameter()
    {
        DisplayName = string.Empty;
        Min = 0;
        Max = 100;
        GridNum = 4;
        bSnapToGrid = bWrapInput = false;
    }

    public FBlendParameter(FStructFallback data) : this()
    {
        DisplayName = data.GetOrDefault<string>(nameof(DisplayName));
        Min = data.GetOrDefault<float>(nameof(Min));
        Max = data.GetOrDefault<float>(nameof(Max));
        GridNum = data.GetOrDefault<int>(nameof(GridNum));
        bSnapToGrid = data.GetOrDefault<bool>(nameof(bSnapToGrid));
        bWrapInput = data.GetOrDefault<bool>(nameof(bWrapInput));
    }

    public float GetRange() => Max - Min;
    public float GetGridSize() => GetRange() / GridNum;
}
