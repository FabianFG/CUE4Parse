using System.IO;
using CUE4Parse.UE4.FMod.Objects;

namespace CUE4Parse.UE4.FMod.Nodes;

public class BankInfoNode
{
    public readonly FModGuid BaseGuid;
    public readonly ulong Hash;
    public readonly int FileVersion;
    public readonly int ExportFlags;

    public BankInfoNode(BinaryReader Ar)
    {
        BaseGuid = new FModGuid(Ar);
        Hash = Ar.ReadUInt64();
        FileVersion = Ar.ReadInt32();
        ExportFlags = Ar.ReadInt32();
    }
}
