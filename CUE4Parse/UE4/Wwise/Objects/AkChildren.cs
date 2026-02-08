using CUE4Parse.UE4.Readers;

namespace CUE4Parse.UE4.Wwise.Objects;

public readonly struct AkChildren
{
    public readonly uint[] ChildIds;

    public AkChildren(FArchive Ar)
    {
        ChildIds = Ar.ReadArray<uint>((int)Ar.Read<uint>());
    }
}
