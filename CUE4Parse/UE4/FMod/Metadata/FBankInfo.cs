using System.IO;
using CUE4Parse.UE4.FMod.Enums;
using CUE4Parse.UE4.FMod.Objects;

namespace CUE4Parse.UE4.FMod.Metadata;

public readonly struct FBankInfo
{
    public readonly FModGuid BaseGuid;
    public readonly ulong Hash;
    public readonly EFModVersion FileVersion;
    public readonly int TopLevelEventCount;
    public readonly int ExportFlags;

    public FBankInfo(BinaryReader Ar)
    {
        BaseGuid = new FModGuid(Ar);
        if (FModReader.Version >= 0x37) Hash = Ar.ReadUInt64();
        FileVersion = (EFModVersion)FModReader.Version;
        if (FModReader.Version >= 0x41) TopLevelEventCount = Ar.ReadInt32();
        if (FModReader.Version >= 0x4D) ExportFlags = Ar.ReadInt32();
    }
}
