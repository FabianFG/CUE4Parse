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
        var leftSpeaker = left.Speaker.ResolvedObject?.GetFullName();
        var rightSpeaker = right.Speaker.ResolvedObject?.GetFullName();
        
        if (!leftSpeaker?.Equals(rightSpeaker, StringComparison.OrdinalIgnoreCase) ?? true)
            return false;

        if (left.Targets.Length != right.Targets.Length)
            return false;

        for (var i = 0; i < left.Targets.Length; i++)
        {
            var leftTarget = left.Targets[i].ResolvedObject?.GetFullName();
            var rightTarget = right.Targets[i].ResolvedObject?.GetFullName();
            
            if (!leftTarget?.Equals(rightTarget, StringComparison.OrdinalIgnoreCase) ?? true)
                return false;
        }
        
        return true;
    }

    public static bool operator !=(FDialogueContext left, FDialogueContext right) => !(left == right);
}