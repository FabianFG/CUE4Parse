using System.IO;
using CUE4Parse.UE4.FMod.Objects;

namespace CUE4Parse.UE4.FMod.Metadata;

public readonly struct FBankInfo
{
    public readonly FModGuid BaseGuid;
    public readonly ulong Hash;
    public readonly int FileVersion;
    public readonly int ExportFlags;

    public FBankInfo(BinaryReader Ar)
    {
        BaseGuid = new FModGuid(Ar);
        Hash = Ar.ReadUInt64();
        FileVersion = Ar.ReadInt32();
        ExportFlags = Ar.ReadInt32();
    }
}
