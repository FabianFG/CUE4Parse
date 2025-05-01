using System.Collections.Generic;
using CUE4Parse.UE4.Readers;
using Newtonsoft.Json;

namespace CUE4Parse.UE4.Wwise.Objects;

public class HierarchyMusicTrack : AbstractHierarchy
{
    public byte Flags { get; private set; }
    public uint NumSources { get; private set; }
    public List<AkBankSourceData> Sources { get; private set; } = new();
    public uint NumPlaylistItems { get; private set; }
    public List<AkTrackSrcInfo> Playlist { get; private set; } = new();

    public HierarchyMusicTrack(FArchive Ar) : base(Ar)
    {
        if (WwiseVersions.WwiseVersion > 89 && WwiseVersions.WwiseVersion <= 112)
        {
            Flags = Ar.Read<byte>();
        }
        else if (WwiseVersions.WwiseVersion <= 152)
        {
            Flags = Ar.Read<byte>();
        }

        NumSources = Ar.Read<uint>();
        if (WwiseVersions.WwiseVersion <= 26)
        {
            for (int i = 0; i < NumSources; i++)
            {
                Sources.Add(new AkBankSourceData(Ar));
            }
        }

        for (int i = 0; i < NumSources; i++)
        {
            Sources.Add(new AkBankSourceData(Ar));
        }

        if (WwiseVersions.WwiseVersion > 26)
        {
            NumPlaylistItems = Ar.Read<uint>();
            for (int i = 0; i < NumPlaylistItems; i++)
            {
                Playlist.Add(new AkTrackSrcInfo(Ar));
            }

            Ar.Read<uint>(); // numSubTrack
        }

        // TODO: read the rest
    }

    public override void WriteJson(JsonWriter writer, JsonSerializer serializer)
    {
        writer.WriteStartObject();

        writer.WritePropertyName("Flags");
        writer.WriteValue(Flags);

        writer.WritePropertyName("NumSources");
        writer.WriteValue(NumSources);

        writer.WritePropertyName("Sources");
        serializer.Serialize(writer, Sources);

        writer.WritePropertyName("NumPlaylistItems");
        writer.WriteValue(NumPlaylistItems);

        writer.WritePropertyName("Playlist");
        serializer.Serialize(writer, Playlist);

        writer.WriteEndObject();
    }
}
