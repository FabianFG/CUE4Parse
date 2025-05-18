using System.Collections.Generic;
using CUE4Parse.UE4.Readers;
using Newtonsoft.Json;

namespace CUE4Parse.UE4.Wwise.Objects.HIRC;

public class HierarchyMusicRandomSequenceContainer : BaseHierarchyMusic
{
    public readonly AkMeterInfo MeterInfo;
    public readonly List<AkStinger> Stingers;
    public readonly AkMusicTransitionRule MusicTransitionRule;
    public readonly List<AkMusicRanSeqPlaylistItem> Playlist;

    public HierarchyMusicRandomSequenceContainer(FArchive Ar) : base(Ar)
    {
        MeterInfo = new AkMeterInfo(Ar);
        Stingers = AkStinger.ReadMultiple(Ar);
        MusicTransitionRule = new AkMusicTransitionRule(Ar);

        Ar.Read<uint>(); // numPlaylistItems, I assume this is for parent and children together, therefore parent is always 1
        Playlist = [];
        for (int i = 0; i < 1; i++)
        {
            var playlistItem = new AkMusicRanSeqPlaylistItem(Ar);
            Playlist.Add(playlistItem);
        }
    }

    public override void WriteJson(JsonWriter writer, JsonSerializer serializer)
    {
        writer.WriteStartObject();

        base.WriteJson(writer, serializer);

        writer.WritePropertyName("MeterInfo");
        serializer.Serialize(writer, MeterInfo);

        writer.WritePropertyName("Stingers");
        serializer.Serialize(writer, Stingers);

        writer.WritePropertyName("MusicTransitionRule");
        serializer.Serialize(writer, MusicTransitionRule.Rules);

        writer.WritePropertyName("Playlist");
        serializer.Serialize(writer, Playlist);

        writer.WriteEndObject();
    }
}
