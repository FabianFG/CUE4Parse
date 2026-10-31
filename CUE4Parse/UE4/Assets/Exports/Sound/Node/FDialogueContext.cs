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
        var leftSpeaker = left.Speaker?.ResolvedObject?.GetFullName();
        var rightSpeaker = right.Speaker?.ResolvedObject?.GetFullName();

        if (!string.Equals(leftSpeaker, rightSpeaker, StringComparison.OrdinalIgnoreCase))
            return false;

        var leftTargets = left.Targets ?? [];
        var rightTargets = right.Targets ?? [];

        if (leftTargets.Length != rightTargets.Length)
            return false;

        for (var i = 0; i < leftTargets.Length; i++)
        {
            var leftTarget = leftTargets[i].ResolvedObject?.GetFullName();
            var rightTarget = rightTargets[i].ResolvedObject?.GetFullName();

            if (!string.Equals(leftTarget, rightTarget, StringComparison.OrdinalIgnoreCase))
                return false;
        }

        return true;
    }

    public static bool operator !=(FDialogueContext left, FDialogueContext right) => !(left == right);

    public override bool Equals(object? obj) => obj is FDialogueContext other && this == other;

    public bool Equals(FDialogueContext other) => this == other;

    public override int GetHashCode()
    {
        var hash = new HashCode();
        hash.Add(Speaker?.ResolvedObject?.GetFullName(), StringComparer.OrdinalIgnoreCase);
        var targets = Targets ?? [];
        hash.Add(targets.Length);
        foreach (var target in targets)
        {
            hash.Add(target.ResolvedObject?.GetFullName(), StringComparer.OrdinalIgnoreCase);
        }
        return hash.ToHashCode();
    }
}
