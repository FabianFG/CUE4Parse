using System.Collections.Generic;
using CUE4Parse.UE4.Readers;

namespace CUE4Parse.UE4.Wwise.Objects;

public class AkMusicTransitionRule
{
    public readonly List<TransitionRule> Rules;

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
    public readonly List<int> SrcIds;
    public readonly List<int> DstIds;
    public readonly List<SrcRule> SrcRules;
    public readonly List<DstRule> DstRules;
    public readonly bool HasTransitionObject;
    public readonly TransitionObject? TransObject;

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
    public readonly int TransitionTime;
    public readonly uint FadeCurve;
    public readonly int FadeOffset;
    public readonly uint SyncType;
    public readonly uint MarkerId;
    public readonly uint CueFilterHash;
    public readonly byte PlayPostExit;

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
    public readonly int TransitionTime;
    public readonly uint FadeCurve;
    public readonly int FadeOffset;
    public readonly uint MarkerID;
    public readonly uint CueFilterHash;
    public readonly uint JumpToId;
    public readonly ushort JumpToType;
    public readonly ushort EntryType;
    public readonly byte PlayPreEntry;
    public readonly byte DestMatchSourceCueName;

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
    public readonly uint SegmentId;
    public readonly FadeParams FadeInParams;
    public readonly FadeParams FadeOutParams;
    public readonly byte PlayPreEntry;
    public readonly byte PlayPostExit;

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
    public readonly int TransitionTime;
    public readonly uint FadeCurve;
    public readonly int FadeOffset;

    public FadeParams(FArchive Ar)
    {
        TransitionTime = Ar.Read<int>();
        FadeCurve = Ar.Read<uint>();
        FadeOffset = Ar.Read<int>();
    }
}
