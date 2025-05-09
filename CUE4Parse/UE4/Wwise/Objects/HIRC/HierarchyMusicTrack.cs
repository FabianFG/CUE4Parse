using System.Collections.Generic;
using CUE4Parse.UE4.Readers;
using Newtonsoft.Json;

namespace CUE4Parse.UE4.Wwise.Objects.HIRC;

public class HierarchyMusicTrack : AbstractHierarchy
{
    public byte Flags { get; private set; }
    public uint NumSources { get; private set; }
    public List<AkBankSourceData> Sources { get; private set; } = [];
    public uint NumPlaylistItems { get; private set; }
    public List<AkTrackSrcInfo> Playlist { get; private set; } = [];
    public List<AkClipAutomation> ClipAutomations { get; private set; } = [];
    public BaseHierarchy BaseParams { get; private set; }
    public short Loop { get; private set; }
    public short LoopModMin { get; private set; }
    public short LoopModMax { get; private set; }
    public uint ERSType { get; private set; }
    public byte ETrackType { get; private set; }
    public int LookAheadTime { get; private set; }

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

        if (WwiseVersions.WwiseVersion > 62)
        {
            var numClipAutomationItems = Ar.Read<uint>();
            for (int i = 0; i < numClipAutomationItems; i++)
            {
                ClipAutomations.Add(new AkClipAutomation(Ar));
            }
        }

        Ar.Position -= 4; // Step back so AbstractHierarchy starts reading correctly, since ID is read twice
        BaseParams = new BaseHierarchy(Ar);

        if (WwiseVersions.WwiseVersion <= 56)
        {
            Loop = Ar.Read<short>();
            LoopModMin = Ar.Read<short>();
            LoopModMax = Ar.Read<short>();
        }

        if (WwiseVersions.WwiseVersion <= 89)
        {
            ERSType = Ar.Read<uint>();
        }
        else
        {
            ETrackType = Ar.Read<byte>();
            if (ETrackType == 0x3) // Special case for track type
            {
                // TODO: implement switch and trans params here
            }
        }

        LookAheadTime = Ar.Read<int>();

        if (WwiseVersions.WwiseVersion <= 26)
        {
            uint numPlaylistItems = Ar.Read<uint>();
            if (numPlaylistItems > 0)
            {
                for (int i = 0; i < numPlaylistItems; i++)
                {
                    var playlistItem = new AkTrackSrcInfo(Ar);
                    Playlist.Add(playlistItem);
                }
            }

            Ar.Read<uint>(); // Unknown flag
        }
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

        writer.WritePropertyName("ClipAutomations");
        serializer.Serialize(writer, ClipAutomations);

        writer.WritePropertyName("BaseParams");
        writer.WriteStartObject();
        BaseParams.WriteJson(writer, serializer);
        writer.WriteEndObject();

        if (Loop != 0)
        {
            writer.WritePropertyName("Loop");
            writer.WriteValue(Loop);
        }

        if (LoopModMin != 0)
        {
            writer.WritePropertyName("LoopModMin");
            writer.WriteValue(LoopModMin);
        }

        if (LoopModMax != 0)
        {
            writer.WritePropertyName("LoopModMax");
            writer.WriteValue(LoopModMax);
        }

        if (ERSType != 0)
        {
            writer.WritePropertyName("ERSType");
            writer.WriteValue(ERSType);
        }

        if (ETrackType != 0)
        {
            writer.WritePropertyName("ETrackType");
            writer.WriteValue(ETrackType);
        }

        if (LookAheadTime != 0)
        {
            writer.WritePropertyName("LookAheadTime");
            writer.WriteValue(LookAheadTime);
        }

        writer.WriteEndObject();
    }
}
