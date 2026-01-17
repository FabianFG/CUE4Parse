using System;
using CUE4Parse.UE4.Assets.Objects;
using CUE4Parse.UE4.Assets.Utils;
using CUE4Parse.UE4.Objects.UObject;

namespace CUE4Parse.UE4.Assets.Exports.Sound.Node;

[StructFallback]
public struct FDialogueContext
{
    public FPackageIndex Speaker;
    public FPackageIndex[] Targets;
    
    public FDialogueContext(FStructFallback fallback)
    {
        Speaker = fallback.GetOrDefault(nameof(Speaker), new FPackageIndex());
        Targets = fallback.GetOrDefault<FPackageIndex[]>(nameof(Targets), []);
    }
    
    public static bool operator ==(FDialogueContext left, FDialogueContext right)
    {
        if (!left.Speaker.ResolvedObject?.GetFullName().Equals(right.Speaker.ResolvedObject?.GetFullName(), StringComparison.OrdinalIgnoreCase) ?? true)
            return false;

        if (left.Targets.Length != right.Targets.Length)
            return false;

        for (int i = 0; i < left.Targets.Length; i++)
        {
            if (left.Targets[i].ResolvedObject?.GetFullName().Equals(right.Targets[i].ResolvedObject?.GetFullName(), StringComparison.OrdinalIgnoreCase) ?? true)
                return false;
        }
        
        return true;
    }

    public static bool operator !=(FDialogueContext left, FDialogueContext right)
    {
        return !(left == right);
    }
}