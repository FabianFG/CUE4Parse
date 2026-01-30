using CUE4Parse.UE4.Readers;
using CUE4Parse.UE4.Wwise.Enums;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace CUE4Parse.UE4.Wwise.Objects;

public struct AkGameSync
{
    public readonly uint GroupId;
    [JsonConverter(typeof(StringEnumConverter))]
    public readonly EGroupType GroupType;

    public AkGameSync(uint groupId, EGroupType groupType)
    {
        GroupId = groupId;
        GroupType = groupType;
    }

    public static AkGameSync[] ReadSequential(FArchive Ar, uint count)
    {
        var groupIds = Ar.ReadArray<uint>((int) count);
        var groupTypes = Ar.ReadArray<EGroupType>((int) count);

        var gameSyncs = new AkGameSync[count];
        for (int i = 0; i < count; i++)
            gameSyncs[i] = new AkGameSync(groupIds[i], groupTypes[i]);

        return gameSyncs;
    }
}
