using System.Collections.Generic;
using CUE4Parse.UE4.Readers;

namespace CUE4Parse.UE4.Wwise.Objects;

public class AkStateProperty
{
    public readonly ushort Id;
    public readonly float Value;

    public AkStateProperty(ushort id, float value)
    {
        Id = id;
        Value = value;
    }
}

public class AkState
{
    public readonly uint Id;
    public readonly uint? StateInstanceId;
    public readonly List<AkStateProperty> Properties;

    public AkState(uint id, uint? stateInstanceId, List<AkStateProperty> properties)
    {
        Id = id;
        StateInstanceId = stateInstanceId;
        Properties = properties;
    }
}

public class AkStateGroup
{
    public readonly uint Id;
    public readonly byte GroupType;
    public readonly List<AkState> States;

    public AkStateGroup(uint id, byte groupType, List<AkState> states)
    {
        Id = id;
        GroupType = groupType;
        States = states;
    }
}

public class AkStateChunk
{
    public readonly int HeaderCount;
    public readonly List<AkStateGroup> Groups;

    public AkStateChunk(FArchive Ar)
    {
        HeaderCount = Ar.Read7BitEncodedInt();
        for (int i = 0; i < HeaderCount; i++)
        {
            Ar.Read7BitEncodedInt();
            Ar.Read<byte>();
            Ar.Read<byte>();
        }

        int groupCount = Ar.Read7BitEncodedInt();
        var groups = new List<AkStateGroup>(groupCount);
        for (int g = 0; g < groupCount; g++)
        {
            uint groupId = Ar.Read<uint>();
            byte groupType = Ar.Read<byte>();
            int stateCount = Ar.Read7BitEncodedInt();

            var states = new List<AkState>(stateCount);
            for (int s = 0; s < stateCount; s++)
            {
                uint stateId = Ar.Read<uint>();
                if (WwiseVersions.Version <= 145)
                {
                    uint stateInstanceId = Ar.Read<uint>();
                    states.Add(new AkState(stateId, stateInstanceId, []));
                }
                else
                {
                    ushort propCount = Ar.Read<ushort>();
                    var props = new List<AkStateProperty>(propCount);
                    for (int k = 0; k < propCount; k++)
                    {
                        ushort propId = Ar.Read<ushort>();
                        float value = Ar.Read<float>();
                        props.Add(new AkStateProperty(propId, value));
                    }
                    states.Add(new AkState(stateId, null, props));
                }
            }

            groups.Add(new AkStateGroup(groupId, groupType, states));
        }

        Groups = groups;
    }
}
