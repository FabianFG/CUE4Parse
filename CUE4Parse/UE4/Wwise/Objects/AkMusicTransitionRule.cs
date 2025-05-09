using System.Collections.Generic;
using CUE4Parse.UE4.Readers;
using Newtonsoft.Json;

namespace CUE4Parse.UE4.Wwise.Objects;

public class AkMusicTransitionRule
{
    public List<TransitionRule> Rules { get; private set; }

    public AkMusicTransitionRule(FArchive Ar)
    {
        var numRules = Ar.Read<uint>();
        Rules = [];

        for (int i = 0; i < numRules; i++)
        {
            var rule = new TransitionRule(Ar);
            Rules.Add(rule);
        }
    }

    public void WriteJson(JsonWriter writer, JsonSerializer serializer)
    {
        writer.WriteStartObject();

        writer.WritePropertyName("Rules");
        serializer.Serialize(writer, Rules);

        writer.WriteEndObject();
    }
}

public class TransitionRule
{
    public List<int> SrcIds { get; private set; }
    public List<int> DstIds { get; private set; }
    public List<SrcRule> SrcRules { get; private set; }
    public List<DstRule> DstRules { get; private set; }
    public bool HasTransitionObject { get; private set; }
    public TransitionObject? TransObject { get; private set; }

    public TransitionRule(FArchive Ar)
    {
        int numSrc = WwiseVersions.WwiseVersion <= 72 ? 1 : Ar.Read<int>();
        SrcIds = [];
        for (int i = 0; i < numSrc; i++)
        {
            SrcIds.Add(Ar.Read<int>());
        }

        int numDst = WwiseVersions.WwiseVersion <= 72 ? 1 : Ar.Read<int>();
        DstIds = [];
        for (int i = 0; i < numDst; i++)
        {
            DstIds.Add(Ar.Read<int>());
        }

        SrcRules = [new SrcRule(Ar)];
        DstRules = [new DstRule(Ar)];

        HasTransitionObject = WwiseVersions.WwiseVersion <= 72 ? Ar.Read<byte>() != 0 : Ar.Read<byte>() != 0;
        if (HasTransitionObject)
        {
            TransObject = new TransitionObject(Ar);
        }
    }

    public void WriteJson(JsonWriter writer, JsonSerializer serializer)
    {
        writer.WriteStartObject();

        writer.WritePropertyName("SrcIds");
        serializer.Serialize(writer, SrcIds);

        writer.WritePropertyName("DstIds");
        serializer.Serialize(writer, DstIds);

        writer.WritePropertyName("SrcRules");
        serializer.Serialize(writer, SrcRules);

        writer.WritePropertyName("DstRules");
        serializer.Serialize(writer, DstRules);

        writer.WritePropertyName("HasTransitionObject");
        writer.WriteValue(HasTransitionObject);

        if (HasTransitionObject)
        {
            writer.WritePropertyName("TransObject");
            TransObject?.WriteJson(writer, serializer);
        }

        writer.WriteEndObject();
    }
}

public class SrcRule
{
    public int TransitionTime { get; private set; }
    public uint FadeCurve { get; private set; }
    public int FadeOffset { get; private set; }
    public uint SyncType { get; private set; }
    public uint MarkerId { get; private set; }
    public uint CueFilterHash { get; private set; }
    public byte PlayPostExit { get; private set; }

    public SrcRule(FArchive Ar)
    {
        TransitionTime = Ar.Read<int>();
        FadeCurve = Ar.Read<uint>();
        FadeOffset = Ar.Read<int>();
        SyncType = Ar.Read<uint>();

        if (WwiseVersions.WwiseVersion > 62 && WwiseVersions.WwiseVersion <= 72)
            MarkerId = Ar.Read<uint>();
        else if (WwiseVersions.WwiseVersion > 72)
            CueFilterHash = Ar.Read<uint>();

        PlayPostExit = Ar.Read<byte>();
    }

    public void WriteJson(JsonWriter writer, JsonSerializer serializer)
    {
        writer.WriteStartObject();

        writer.WritePropertyName("TransitionTime");
        writer.WriteValue(TransitionTime);

        writer.WritePropertyName("FadeCurve");
        writer.WriteValue(FadeCurve);

        writer.WritePropertyName("FadeOffset");
        writer.WriteValue(FadeOffset);

        writer.WritePropertyName("SyncType");
        writer.WriteValue(SyncType);

        writer.WritePropertyName("MarkerId");
        writer.WriteValue(MarkerId);

        writer.WritePropertyName("CueFilterHash");
        writer.WriteValue(CueFilterHash);

        writer.WritePropertyName("PlayPostExit");
        writer.WriteValue(PlayPostExit);

        writer.WriteEndObject();
    }
}

public class DstRule
{
    public int TransitionTime { get; private set; }
    public uint FadeCurve { get; private set; }
    public int FadeOffset { get; private set; }
    public uint MarkerID { get; private set; }
    public uint CueFilterHash { get; private set; }
    public uint JumpToId { get; private set; }
    public ushort JumpToType { get; private set; }
    public ushort EntryType { get; private set; }
    public byte PlayPreEntry { get; private set; }
    public byte DestMatchSourceCueName { get; private set; }

    public DstRule(FArchive Ar)
    {
        TransitionTime = Ar.Read<int>();
        FadeCurve = Ar.Read<uint>();
        FadeOffset = Ar.Read<int>();

        if (WwiseVersions.WwiseVersion <= 72)
            MarkerID = Ar.Read<uint>();
        else
            CueFilterHash = Ar.Read<uint>();

        JumpToId = Ar.Read<uint>();

        if (WwiseVersions.WwiseVersion > 132)
        {
            JumpToType = Ar.Read<ushort>();
            EntryType = Ar.Read<ushort>();
        }

        PlayPreEntry = Ar.Read<byte>();

        if (WwiseVersions.WwiseVersion > 62)
            DestMatchSourceCueName = Ar.Read<byte>();
    }

    public void WriteJson(JsonWriter writer, JsonSerializer serializer)
    {
        writer.WriteStartObject();

        writer.WritePropertyName("TransitionTime");
        writer.WriteValue(TransitionTime);

        writer.WritePropertyName("FadeCurve");
        writer.WriteValue(FadeCurve);

        writer.WritePropertyName("FadeOffset");
        writer.WriteValue(FadeOffset);

        writer.WritePropertyName("MarkerID");
        writer.WriteValue(MarkerID);

        writer.WritePropertyName("CueFilterHash");
        writer.WriteValue(CueFilterHash);

        writer.WritePropertyName("JumpToId");
        writer.WriteValue(JumpToId);

        writer.WritePropertyName("JumpToType");
        writer.WriteValue(JumpToType);

        writer.WritePropertyName("EntryType");
        writer.WriteValue(EntryType);

        writer.WritePropertyName("PlayPreEntry");
        writer.WriteValue(PlayPreEntry);

        writer.WritePropertyName("DestMatchSourceCueName");
        writer.WriteValue(DestMatchSourceCueName);

        writer.WriteEndObject();
    }
}

public class TransitionObject
{
    public uint SegmentId { get; private set; }
    public FadeParams FadeInParams { get; private set; }
    public FadeParams FadeOutParams { get; private set; }
    public byte PlayPreEntry { get; private set; }
    public byte PlayPostExit { get; private set; }

    public TransitionObject(FArchive Ar)
    {
        SegmentId = Ar.Read<uint>();
        FadeInParams = new FadeParams(Ar);
        FadeOutParams = new FadeParams(Ar);
        PlayPreEntry = Ar.Read<byte>();
        PlayPostExit = Ar.Read<byte>();
    }

    public void WriteJson(JsonWriter writer, JsonSerializer serializer)
    {
        writer.WriteStartObject();

        writer.WritePropertyName("SegmentId");
        writer.WriteValue(SegmentId);

        writer.WritePropertyName("FadeInParams");
        FadeInParams.WriteJson(writer, serializer);

        writer.WritePropertyName("FadeOutParams");
        FadeOutParams.WriteJson(writer, serializer);

        writer.WritePropertyName("PlayPreEntry");
        writer.WriteValue(PlayPreEntry);

        writer.WritePropertyName("PlayPostExit");
        writer.WriteValue(PlayPostExit);

        writer.WriteEndObject();
    }
}

public class FadeParams
{
    public int TransitionTime { get; private set; }
    public uint FadeCurve { get; private set; }
    public int FadeOffset { get; private set; }

    public FadeParams(FArchive Ar)
    {
        TransitionTime = Ar.Read<int>();
        FadeCurve = Ar.Read<uint>();
        FadeOffset = Ar.Read<int>();
    }

    public void WriteJson(JsonWriter writer, JsonSerializer serializer)
    {
        writer.WriteStartObject();

        writer.WritePropertyName("TransitionTime");
        writer.WriteValue(TransitionTime);

        writer.WritePropertyName("FadeCurve");
        writer.WriteValue(FadeCurve);

        writer.WritePropertyName("FadeOffset");
        writer.WriteValue(FadeOffset);

        writer.WriteEndObject();
    }
}
