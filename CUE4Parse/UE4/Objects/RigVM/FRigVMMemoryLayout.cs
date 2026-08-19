using CUE4Parse.UE4.Readers;
using CUE4Parse.UE4.Versions;

namespace CUE4Parse.UE4.Objects.RigVM;

// Which optional fields a RigVM serializes, for the UClass-based storage era (4.25 - 5.0). All of them are
// gated on custom versions that unversioned cooked packages don't carry, so they get inferred from EGame
// which can't tell a release from a game built off a dev snapshot of it. Fortnite 14.40 predates the 4.26
// release by months, so URigVM probes these against the bytes rather than trusting the inferred version.
public readonly struct FRigVMMemoryLayout(FAnimObjectVersion.Type animVersion, bool bSerializeOffsetSegmentPaths)
{
    public readonly FAnimObjectVersion.Type AnimVersion = animVersion;
    public readonly bool bSerializeOffsetSegmentPaths = bSerializeOffsetSegmentPaths;

    // Most likely first: what the versions imply, then what a snapshot build could have shipped with
    public static FRigVMMemoryLayout[] GetCandidates(FArchive Ar)
    {
        var animVersion = FAnimObjectVersion.Get(Ar);
        var bOffsetSegmentPaths = FReleaseObjectVersion.Get(Ar) >= FReleaseObjectVersion.Type.SerializeRigVMOffsetSegmentPaths;

        // Pinned just below the first RigVM change a pre-release build could be missing
        const FAnimObjectVersion.Type snapshot = FAnimObjectVersion.Type.NotifyAndSyncMarkerGuids;

        return
        [
            new FRigVMMemoryLayout(animVersion, bOffsetSegmentPaths),
            new FRigVMMemoryLayout(snapshot, false),
            new FRigVMMemoryLayout(snapshot, true),
            new FRigVMMemoryLayout(animVersion, !bOffsetSegmentPaths)
        ];
    }

    public override string ToString() => $"{nameof(AnimVersion)}: {AnimVersion}, {nameof(bSerializeOffsetSegmentPaths)}: {bSerializeOffsetSegmentPaths}";
}
