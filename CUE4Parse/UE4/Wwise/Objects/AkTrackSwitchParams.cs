using CUE4Parse.UE4.Readers;
using CUE4Parse.UE4.Wwise.Enums;

namespace CUE4Parse.UE4.Wwise.Objects;

public class AkTrackSwitchParams
{
    public readonly EAkGroupType GroupType;
    public readonly uint GroupId;
    public readonly uint DefaultSwitch;
    public readonly uint[] SwitchAssociationIds;

    public AkTrackSwitchParams(FArchive Ar)
    {
        GroupType = Ar.Read<EAkGroupType>();
        GroupId = Ar.Read<uint>();
        DefaultSwitch = Ar.Read<uint>();
        SwitchAssociationIds = Ar.ReadArray<uint>((int) Ar.Read<uint>());
    }
}
