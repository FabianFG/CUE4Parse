using System.Collections.Generic;
using CUE4Parse.UE4.Readers;

namespace CUE4Parse.UE4.Wwise.Objects;

public class AkPlayList
{
    public readonly List<AkPlayListItem> PlaylistItems;

    public AkPlayList(FArchive Ar)
    {
        var numItems = WwiseVersions.Version <= 38 ? Ar.Read<uint>() : Ar.Read<ushort>();

        PlaylistItems = new List<AkPlayListItem>((int) numItems);

        for (var i = 0; i < numItems; i++)
        {
            PlaylistItems.Add(new AkPlayListItem(Ar));
        }
    }

    public class AkPlayListItem
    {
        public readonly uint PlayId;
        public readonly int Weight;

        public AkPlayListItem(FArchive Ar)
        {
            PlayId = Ar.Read<uint>();
            Weight = WwiseVersions.Version <= 56 ? Ar.Read<byte>() : Ar.Read<int>(); // Could also be uint for version 128
        }
    }
}
