namespace CUE4Parse.UE4.Wwise.Objects;

public readonly struct AkPlayList
{
    public readonly AkPlayListItem[] PlaylistItems;

    public AkPlayList(FWwiseArchive Ar)
    {
        var numItems = Ar.Version <= 38 ? Ar.Read<uint>() : Ar.Read<ushort>();
        PlaylistItems = Ar.ReadArray((int) numItems, () => new AkPlayListItem(Ar));
    }
}

public readonly struct AkPlayListItem
{
    public readonly uint PlayId;
    public readonly int Weight;

    public AkPlayListItem(FWwiseArchive Ar)
    {
        PlayId = Ar.Read<uint>();
        Weight = Ar.Version <= 56 ? Ar.Read<byte>() : Ar.Read<int>(); // Could also be uint for version 128
    }
}
