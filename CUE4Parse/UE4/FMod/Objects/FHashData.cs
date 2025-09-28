using System.IO;

namespace CUE4Parse.UE4.FMod.Objects;

public readonly struct FHashData
{
    public readonly FModGuid Guid;
    public readonly uint Hash;

    public FHashData(BinaryReader Ar)
    {
        Guid = new FModGuid(Ar);
        Hash = Ar.ReadUInt32();
    }
}
