using CUE4Parse.UE4.Wwise.Enums;

namespace CUE4Parse.UE4.Wwise.Objects;

public readonly struct AkMusicTransitionRule(FWwiseArchive Ar)
{
    public readonly TransitionRule[] Rules = Ar.ReadArray((int) Ar.Read<uint>(), () => new TransitionRule(Ar));
}

public readonly struct TransitionRule
{
    public readonly int[] SrcIds;
    public readonly int[] DestIds;
    public readonly AkMusicTransSrcRule SrcRules;
    public readonly AkMusicTransDestRule DestRules;
    public readonly AkMusicTransitionObject? TransObject;

    public TransitionRule(FWwiseArchive Ar)
    {
        int numSrc = Ar.Version <= 72 ? 1 : Ar.Read<int>();
        SrcIds = Ar.ReadArray<int>(numSrc);

        int numDest = Ar.Version <= 72 ? 1 : Ar.Read<int>();
        DestIds = Ar.ReadArray<int>(numDest);

        SrcRules = new AkMusicTransSrcRule(Ar);
        DestRules = new AkMusicTransDestRule(Ar);

        bool hasTransitionObject = Ar.Version <= 72 ? Ar.Read<byte>() != 0 : Ar.Read<byte>() != 0;
        if (hasTransitionObject)
        {
            TransObject = new AkMusicTransitionObject(Ar);
        }
    }
}

public readonly struct AkMusicTransSrcRule
{
    public readonly int TransitionTime;
    public readonly EAkCurveInterpolation FadeCurve;
    public readonly int FadeOffset;
    public readonly EAkSyncType SyncType;
    public readonly uint MarkerId;
    public readonly uint CueFilterHash;
    public readonly bool PlayPostExit;

    public AkMusicTransSrcRule(FWwiseArchive Ar)
    {
        TransitionTime = Ar.Read<int>();
        FadeCurve = (EAkCurveInterpolation) Ar.Read<uint>();
        FadeOffset = Ar.Read<int>();
        SyncType = Ar.Read<EAkSyncType>();

        if (Ar.Version > 62 && Ar.Version <= 72)
            MarkerId = Ar.Read<uint>();
        else if (Ar.Version > 72)
            CueFilterHash = Ar.Read<uint>();

        PlayPostExit = Ar.Read<byte>() != 0;
    }
}

public readonly struct AkMusicTransDestRule
{
    public readonly int TransitionTime;
    public readonly EAkCurveInterpolation FadeCurve;
    public readonly int FadeOffset;
    public readonly uint MarkerId;
    public readonly uint CueFilterHash;
    public readonly uint JumpToId;
    public readonly EAkJumpToSelType JumpToType;
    public readonly EAkEntryType EntryType;
    public readonly bool PlayPreEntry;
    public readonly bool DestMatchSourceCueName;

    public AkMusicTransDestRule(FWwiseArchive Ar)
    {
        TransitionTime = Ar.Read<int>();
        FadeCurve = (EAkCurveInterpolation) Ar.Read<uint>();
        FadeOffset = Ar.Read<int>();

        if (Ar.Version <= 72)
            MarkerId = Ar.Read<uint>();
        else
            CueFilterHash = Ar.Read<uint>();

        JumpToId = Ar.Read<uint>();

        if (Ar.Version > 132)
            JumpToType = (EAkJumpToSelType) Ar.Read<ushort>();

        EntryType = (EAkEntryType) Ar.Read<ushort>();
        PlayPreEntry = Ar.Read<byte>() != 0;

        if (Ar.Version > 62)
            DestMatchSourceCueName = Ar.Read<byte>() != 0;
    }
}

public readonly struct AkMusicTransitionObject
{
    public readonly uint SegmentId;
    public readonly AkMusicFade FadeInParams;
    public readonly AkMusicFade FadeOutParams;
    public readonly bool PlayPreEntry;
    public readonly bool PlayPostExit;

    public AkMusicTransitionObject(FWwiseArchive Ar)
    {
        SegmentId = Ar.Read<uint>();
        FadeInParams = new AkMusicFade(Ar);
        FadeOutParams = new AkMusicFade(Ar);
        PlayPreEntry = Ar.Read<byte>() != 0;
        PlayPostExit = Ar.Read<byte>() != 0;
    }
}
