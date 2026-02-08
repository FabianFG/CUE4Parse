using CUE4Parse.UE4.Readers;
using CUE4Parse.UE4.Wwise.Enums;

namespace CUE4Parse.UE4.Wwise.Objects;

public class AkStateChunk
{
    public readonly AkStateGroup[] Groups = [];

    public AkStateChunk(FArchive Ar)
    {
        var numGroups = Ar.Read<uint>();
        Groups = new AkStateGroup[(int) numGroups];
        for (int i = 0; i < numGroups; i++)
        {
            var groupId = Ar.Read<uint>();
            var stateSyncType = (EAkSyncType) Ar.Read<byte>();
            var numStates = Ar.Read<ushort>();

            var states = new AkState[numStates];
            for (int s = 0; s < numStates; s++)
            {
                uint stateId = Ar.Read<uint>();
                uint stateInstanceId = Ar.Read<uint>();
                states[s] = new AkState(stateId, stateInstanceId, []);
            }

            Groups[i] = new AkStateGroup(groupId, stateSyncType, states);
        }
    }
}

public class AkStateAwareChunk
{
    public readonly AkStatePropertyInfo[] StateProperties;
    public readonly AkStateGroup[] Groups;

    // CAkStateAware::ReadStateChunk
    public AkStateAwareChunk(FArchive Ar)
    {
        StateProperties = Ar.ReadArray(WwiseReader.Read7BitEncodedIntBE(Ar), () => new AkStatePropertyInfo(Ar));

        int groupCount = WwiseReader.Read7BitEncodedIntBE(Ar);
        Groups = new AkStateGroup[groupCount];
        for (int g = 0; g < groupCount; g++)
        {
            uint groupId = Ar.Read<uint>();

            if (WwiseVersions.Version > 154)
            {
                var groupUsageId = Ar.Read<uint>();
            }

            var stateSyncType = (EAkSyncType) Ar.Read<byte>();

            int stateCount = WwiseReader.Read7BitEncodedIntBE(Ar);
            var states = new AkState[stateCount];
            for (int s = 0; s < stateCount; s++)
            {
                uint stateId = Ar.Read<uint>();
                if (WwiseVersions.Version <= 145)
                {
                    uint stateInstanceId = Ar.Read<uint>();
                    states[s] = new AkState(stateId, stateInstanceId, []);
                }
                else
                {
                    ushort propCount = Ar.Read<ushort>();
                    var props = new AkStateProperty[propCount];
                    for (int k = 0; k < propCount; k++)
                    {
                        ushort propId = Ar.Read<ushort>();
                        float value = Ar.Read<float>();
                        props[k] = new AkStateProperty(propId, value);
                    }
                    states[s] = new AkState(stateId, null, props);
                }
            }

            Groups[g] = new AkStateGroup(groupId, stateSyncType, states);
        }
    }
}

public readonly struct AkStateProperty(ushort id, float value)
{
    public readonly ushort Id = id;
    public readonly float Value = value;
}

public readonly struct AkState(uint id, uint? stateInstanceId, AkStateProperty[] properties)
{
    public readonly uint Id = id;
    public readonly uint? StateInstanceId = stateInstanceId;
    public readonly AkStateProperty[] Properties = properties;
}

public readonly struct AkStateGroup(uint id, EAkSyncType groupType, AkState[] states)
{
    public readonly uint Id = id;
    public readonly EAkSyncType GroupType = groupType;
    public readonly AkState[] States = states;
}
