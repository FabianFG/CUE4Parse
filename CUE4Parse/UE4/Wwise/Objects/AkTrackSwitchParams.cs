using CUE4Parse.UE4.Readers;
using CUE4Parse.UE4.Wwise.Enums;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace CUE4Parse.UE4.Wwise.Objects;

public class AkTrackSwitchParams
{
    [JsonConverter(typeof(StringEnumConverter))]
    public EGroupType GroupType { get; protected set; }
    public uint GroupId { get; protected set; }
    public uint DefaultSwitch { get; protected set; }
    public uint[] SwitchAssociationIds { get; protected set; }

    public AkTrackSwitchParams(FArchive Ar)
    {
        GroupType = Ar.Read<EGroupType>();
        GroupId = Ar.Read<uint>();
        DefaultSwitch = Ar.Read<uint>();

        uint numSwitchAssociations = Ar.Read<uint>();
        SwitchAssociationIds = Ar.ReadArray<uint>((int)numSwitchAssociations);
    }
}
