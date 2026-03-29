using System;

namespace CUE4Parse.Utils;

internal static class StringComparerUtils
{
    internal static StringComparison ToComparison(this StringComparer comparer)
    {
        if (comparer == StringComparer.Ordinal) return StringComparison.Ordinal;
        if (comparer == StringComparer.OrdinalIgnoreCase) return StringComparison.OrdinalIgnoreCase;
        if (comparer == StringComparer.CurrentCulture) return StringComparison.CurrentCulture;
        if (comparer == StringComparer.CurrentCultureIgnoreCase) return StringComparison.CurrentCultureIgnoreCase;
        if (comparer == StringComparer.InvariantCulture) return StringComparison.InvariantCulture;
        if (comparer == StringComparer.InvariantCultureIgnoreCase) return StringComparison.InvariantCultureIgnoreCase;
        throw new ArgumentException($"No StringComparison equivalent for comparer '{comparer}'.");
    }
}
