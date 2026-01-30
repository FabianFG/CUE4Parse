using CUE4Parse.UE4.Readers;

namespace CUE4Parse.UE4.Wwise.Objects;

public readonly struct AkMusicTransitionRule
{
    public readonly TransitionRule[] Rules;

    public AkMusicTransitionRule(FArchive Ar)
    {
        Rules = Ar.ReadArray((int) Ar.Read<uint>(), () => new TransitionRule(Ar));
    }
}

public readonly struct TransitionRule
{
    public readonly int[] SrcIds;
    public readonly int[] DestIds;
    public readonly AkMusicTransSrcRule SrcRules;
    public readonly AkMusicTransDestRule DestRules;
    public readonly AkMusicTransitionObject? TransObject;

    public TransitionRule(FArchive Ar)
    {
        int numSrc = WwiseVersions.Version <= 72 ? 1 : Ar.Read<int>();
        SrcIds = Ar.ReadArray<int>(numSrc);

        int numDest = WwiseVersions.Version <= 72 ? 1 : Ar.Read<int>();
        DestIds = Ar.ReadArray<int>(numDest);

        SrcRules = new AkMusicTransSrcRule(Ar);
        DestRules = new AkMusicTransDestRule(Ar);

        bool hasTransitionObject = WwiseVersions.Version <= 72 ? Ar.Read<byte>() != 0 : Ar.Read<byte>() != 0;
        if (hasTransitionObject)
        {
            TransObject = new AkMusicTransitionObject(Ar);
        }
    }
}

public readonly struct AkMusicTransSrcRule
{
    public readonly int TransitionTime;
    public readonly uint FadeCurve;
    public readonly int FadeOffset;
    public readonly uint SyncType;
    public readonly uint MarkerId;
    public readonly uint CueFilterHash;
    public readonly byte PlayPostExit;

    public AkMusicTransSrcRule(FArchive Ar)
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

public readonly struct AkMusicTransDestRule
{
    public readonly int TransitionTime;
    public readonly uint FadeCurve;
    public readonly int FadeOffset;
    public readonly uint MarkerId;
    public readonly uint CueFilterHash;
    public readonly uint JumpToId;
    public readonly ushort JumpToType;
    public readonly ushort EntryType;
    public readonly byte PlayPreEntry;
    public readonly byte DestMatchSourceCueName;

    public AkMusicTransDestRule(FArchive Ar)
    {
        TransitionTime = Ar.Read<int>();
        FadeCurve = Ar.Read<uint>();
        FadeOffset = Ar.Read<int>();

        if (WwiseVersions.Version <= 72)
            MarkerId = Ar.Read<uint>();
        else
            CueFilterHash = Ar.Read<uint>();

        JumpToId = Ar.Read<uint>();

        if (WwiseVersions.Version > 132)
            JumpToType = Ar.Read<ushort>();

        EntryType = Ar.Read<ushort>();
        PlayPreEntry = Ar.Read<byte>();

        if (WwiseVersions.Version > 62)
            DestMatchSourceCueName = Ar.Read<byte>();
    }
}

public readonly struct AkMusicTransitionObject
{
    public readonly uint SegmentId;
    public readonly AkMusicFade FadeInParams;
    public readonly AkMusicFade FadeOutParams;
    public readonly byte PlayPreEntry;
    public readonly byte PlayPostExit;

    public AkMusicTransitionObject(FArchive Ar)
    {
        SegmentId = Ar.Read<uint>();
        FadeInParams = new AkMusicFade(Ar);
        FadeOutParams = new AkMusicFade(Ar);
        PlayPreEntry = Ar.Read<byte>();
        PlayPostExit = Ar.Read<byte>();
    }
}
