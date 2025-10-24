using System.IO;
using CUE4Parse.UE4.FMod.Objects;

namespace CUE4Parse.UE4.FMod.Metadata;

public readonly struct FHashInfo
{
    public readonly FModGuid Guid;
    public readonly uint Hash;

    public FHashInfo(BinaryReader Ar)
    {
        Guid = new FModGuid(Ar);
        Hash = Ar.ReadUInt32();
    }
}
