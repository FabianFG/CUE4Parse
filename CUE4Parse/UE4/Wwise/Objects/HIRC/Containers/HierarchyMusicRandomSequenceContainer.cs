using CUE4Parse.UE4.Readers;
using Newtonsoft.Json;

namespace CUE4Parse.UE4.Wwise.Objects.HIRC.Containers;

public class HierarchyMusicRandomSequenceContainer : BaseHierarchyMusic
{
    public readonly AkMeterInfo MeterInfo;
    public readonly AkStinger[] Stingers;
    public readonly AkMusicTransitionRule MusicTransitionRule;
    public readonly AkMusicRanSeqPlaylistItem[] Playlist;

    // CAkBankMgr::StdBankRead<CAkMusicRanSeqCntr>
    public HierarchyMusicRandomSequenceContainer(FArchive Ar) : base(Ar)
    {
        MeterInfo = new AkMeterInfo(Ar);
        Stingers = AkStinger.ReadArray(Ar);
        MusicTransitionRule = new AkMusicTransitionRule(Ar);

        Ar.Read<uint>(); // numPlaylistItems, I assume this is for parent and children together, therefore parent is always 1
        const int numPlaylistItems = 1;
        Playlist = Ar.ReadArray(numPlaylistItems, () => new AkMusicRanSeqPlaylistItem(Ar));
    }

    public override void WriteJson(JsonWriter writer, JsonSerializer serializer)
    {
        writer.WriteStartObject();

        base.WriteJson(writer, serializer);

        writer.WritePropertyName(nameof(MeterInfo));
        serializer.Serialize(writer, MeterInfo);

        writer.WritePropertyName(nameof(Stingers));
        serializer.Serialize(writer, Stingers);

        writer.WritePropertyName(nameof(MusicTransitionRule));
        serializer.Serialize(writer, MusicTransitionRule.Rules);

        writer.WritePropertyName(nameof(Playlist));
        serializer.Serialize(writer, Playlist);

        writer.WriteEndObject();
    }
}
