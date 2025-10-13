using System.IO;

namespace CUE4Parse.UE4.FMod.Objects;

public readonly struct FSnapshot
{
    public readonly FModGuid SnapshotGuid;
    public readonly uint EntryIndex;
    public readonly uint TargetIndex;
    public readonly float Value;

    public FSnapshot(BinaryReader Ar)
    {
        SnapshotGuid = new FModGuid(Ar);
        EntryIndex = Ar.ReadUInt32();
        TargetIndex = Ar.ReadUInt32();
        Value = Ar.ReadSingle();
    }
}
