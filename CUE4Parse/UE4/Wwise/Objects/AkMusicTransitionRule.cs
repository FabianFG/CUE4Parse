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
        int numSrc = WwiseVersions.Version <= 72 ? 1 : Ar.Read<int>();
        SrcIds = [];
        for (int i = 0; i < numSrc; i++)
        {
            SrcIds.Add(Ar.Read<int>());
        }

        int numDst = WwiseVersions.Version <= 72 ? 1 : Ar.Read<int>();
        DstIds = [];
        for (int i = 0; i < numDst; i++)
        {
            DstIds.Add(Ar.Read<int>());
        }

        SrcRules = [new SrcRule(Ar)];
        DstRules = [new DstRule(Ar)];

        HasTransitionObject = WwiseVersions.Version <= 72 ? Ar.Read<byte>() != 0 : Ar.Read<byte>() != 0;
        if (HasTransitionObject)
        {
            TransObject = new TransitionObject(Ar);
        }
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

        if (WwiseVersions.Version > 62 && WwiseVersions.Version <= 72)
            MarkerId = Ar.Read<uint>();
        else if (WwiseVersions.Version > 72)
            CueFilterHash = Ar.Read<uint>();

        PlayPostExit = Ar.Read<byte>();
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

        if (WwiseVersions.Version <= 72)
            MarkerID = Ar.Read<uint>();
        else
            CueFilterHash = Ar.Read<uint>();

        JumpToId = Ar.Read<uint>();

        if (WwiseVersions.Version > 132)
        {
            JumpToType = Ar.Read<ushort>();
            EntryType = Ar.Read<ushort>();
        }

        PlayPreEntry = Ar.Read<byte>();

        if (WwiseVersions.Version > 62)
            DestMatchSourceCueName = Ar.Read<byte>();
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
}
