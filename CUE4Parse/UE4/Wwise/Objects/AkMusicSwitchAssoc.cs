namespace CUE4Parse.UE4.Wwise.Objects;

public readonly struct AkMusicSwitchAssoc
{
    public readonly uint SwitchId;
    public readonly uint NodeId;

    public AkMusicSwitchAssoc(FWwiseArchive Ar)
    {
        SwitchId = Ar.Read<uint>();
        NodeId = Ar.Read<uint>();
    }
}
