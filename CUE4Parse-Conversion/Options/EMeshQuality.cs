using System;
using System.ComponentModel;

namespace CUE4Parse_Conversion.Options;

public enum EMeshQuality
{
    [Description("Highest Quality Only")]
    Highest,
    [Description("Lowest Quality Only")]
    Lowest,
    [Description("All Qualities (Individual Files)")]
    All
}

internal static class MeshQualityExtensions
{
    internal static IReadOnlyList<uint> GetRange(this EMeshQuality quality, int count, Func<uint, bool> skipPredicate)
    {
        var indices = new List<uint>();
        for (var i = 0u; i < count; i++)
        {
            if (skipPredicate(i)) continue;
            indices.Add(i);
        }

        if (indices.Count > 1)
        {
            switch (quality)
            {
                case EMeshQuality.Highest:
                    indices.RemoveRange(1, indices.Count - 1);
                    break;
                case EMeshQuality.Lowest:
                    indices.RemoveRange(0, indices.Count - 1);
                    break;
            }
        }

        return indices;
    }
}
