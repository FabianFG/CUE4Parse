using System;
using System.ComponentModel;

namespace CUE4Parse_Conversion.V2.Options;

public enum EMeshQuality
{
    [Description("Highest Quality Only")]
    Highest,
    [Description("Lowest Quality Only")]
    Lowest,
    [Description("All Qualities")]
    All
}

internal static class MeshQualityExtensions
{
    internal static (int start, int end) GetRange(this EMeshQuality quality, int count) => quality switch
    {
        EMeshQuality.Highest => (0, Math.Min(1, count)),
        EMeshQuality.Lowest => (Math.Max(0, count - 1), count),
        _ => (0, count) // All
    };
}
