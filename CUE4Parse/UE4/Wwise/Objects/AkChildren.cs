namespace CUE4Parse.UE4.Wwise.Objects;

public readonly struct AkChildren
{
    public readonly uint[] ChildIds;

    public AkChildren(FWwiseArchive Ar)
    {
        ChildIds = Ar.ReadArray<uint>((int)Ar.Read<uint>());
    }
}
