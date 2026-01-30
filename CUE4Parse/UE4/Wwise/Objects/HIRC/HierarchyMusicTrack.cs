using System.Collections.Generic;
using CUE4Parse.UE4.Readers;
using CUE4Parse.UE4.Wwise.Enums;
using Newtonsoft.Json;

namespace CUE4Parse.UE4.Wwise.Objects.HIRC;

public class HierarchyMusicTrack : AbstractHierarchy
{
    public readonly EMusicFlags MusicFlags;
    public readonly AkBankSourceData[] Sources = [];
    public readonly AkTrackSrcInfo[] Playlist = [];
    public readonly AkClipAutomation[] ClipAutomations = [];
    public readonly BaseHierarchy BaseParams;
    public readonly short Loop;
    public readonly short LoopModMin;
    public readonly short LoopModMax;
    public readonly uint ERSType;
    public readonly EMusicTrackType MusicTrackType;
    public readonly AkTrackSwitchParams? SwitchParams;
    public readonly AkMusicTrackTransRule? TransParams;
    public readonly int LookAheadTime;

    public HierarchyMusicTrack(FArchive Ar) : base(Ar)
    {
        if (WwiseVersions.Version > 89 && WwiseVersions.Version <= 112)
        {
            MusicFlags = Ar.Read<EMusicFlags>();
        }
        else if (WwiseVersions.Version <= 152)
        {
            MusicFlags = Ar.Read<EMusicFlags>();
        }

        var numSources = Ar.Read<uint>();
        Sources = new AkBankSourceData[numSources];
        if (WwiseVersions.Version <= 26)
        {
            for (int i = 0; i < numSources; i++)
            {
                Sources[i] = new AkBankSourceData(Ar);
            }
        }

        for (int i = 0; i < numSources; i++)
        {
            Sources[i] = new AkBankSourceData(Ar);
        }

        if (WwiseVersions.Version > 152)
        {
            MusicFlags = Ar.Read<EMusicFlags>();
        }

        if (WwiseVersions.Version > 26)
        {
            var numPlaylistItems = Ar.Read<uint>();
            if (numPlaylistItems > 0)
            {
                Playlist = new AkTrackSrcInfo[numPlaylistItems];
                for (int i = 0; i < numPlaylistItems; i++)
                {
                    Playlist[i] = new AkTrackSrcInfo(Ar);
                }

                Ar.Read<uint>(); // numSubTrack
            }
        }

        if (WwiseVersions.Version > 62)
        {
            var numClipAutomationItems = Ar.Read<uint>();
            ClipAutomations = new AkClipAutomation[numClipAutomationItems];
            for (int i = 0; i < numClipAutomationItems; i++)
            {
                ClipAutomations[i] = new AkClipAutomation(Ar);
            }
        }

        Ar.Position -= 4; // Step back so AbstractHierarchy starts reading correctly, since ID is read twice
        BaseParams = new BaseHierarchy(Ar);

        if (WwiseVersions.Version <= 56)
        {
            Loop = Ar.Read<short>();
            LoopModMin = Ar.Read<short>();
            LoopModMax = Ar.Read<short>();
        }

        if (WwiseVersions.Version <= 89)
        {
            ERSType = Ar.Read<uint>();
        }
        else
        {
            MusicTrackType = Ar.Read<EMusicTrackType>();
            if (MusicTrackType == EMusicTrackType.Switch) // Special case for track type
            {
                SwitchParams = new AkTrackSwitchParams(Ar);
                TransParams = new AkMusicTrackTransRule(Ar);
            }
        }

        LookAheadTime = Ar.Read<int>();

        if (WwiseVersions.Version <= 26)
        {
            uint numPlaylistItems = Ar.Read<uint>();
            if (numPlaylistItems > 0)
            {
                Playlist = new AkTrackSrcInfo[numPlaylistItems];
                for (int i = 0; i < numPlaylistItems; i++)
                {
                    Playlist[i] = new AkTrackSrcInfo(Ar);
                }
            }

            Ar.Read<uint>(); // Unknown flag
        }
    }

    public override void WriteJson(JsonWriter writer, JsonSerializer serializer)
    {
        writer.WriteStartObject();

        writer.WritePropertyName("MusicFlags");
        writer.WriteValue(MusicFlags.ToString());

        writer.WritePropertyName("Sources");
        serializer.Serialize(writer, Sources);

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

        if (MusicTrackType != 0)
        {
            writer.WritePropertyName("MusicTrackType");
            writer.WriteValue(MusicTrackType.ToString());
        }

        if (MusicTrackType == EMusicTrackType.Switch)
        {
            writer.WritePropertyName("SwitchParams");
            serializer.Serialize(writer, SwitchParams);

            writer.WritePropertyName("TransParams");
            serializer.Serialize(writer, TransParams);
        }

        if (LookAheadTime != 0)
        {
            writer.WritePropertyName("LookAheadTime");
            writer.WriteValue(LookAheadTime);
        }

        writer.WriteEndObject();
    }
}
