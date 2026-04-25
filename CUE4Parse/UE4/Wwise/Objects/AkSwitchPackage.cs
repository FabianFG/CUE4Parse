namespace CUE4Parse.UE4.Wwise.Objects;

public readonly struct AkSwitchPackage
{
    public readonly uint SwitchId;
    public readonly uint[] NodeIds;

    public AkSwitchPackage(FWwiseArchive Ar)
    {
        SwitchId = Ar.Read<uint>();
        NodeIds = Ar.ReadArray<uint>((int) Ar.Read<uint>());
    }
}
