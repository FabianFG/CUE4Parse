using System.Collections.Generic;
using CUE4Parse.UE4.Readers;

namespace CUE4Parse.UE4.Wwise.Objects;

public class AkStateChunk
{
    public readonly List<AkStateGroup> Groups = [];

    public AkStateChunk(FArchive Ar)
    {
        var numGroups = Ar.Read<uint>();
        Groups = new List<AkStateGroup>((int) numGroups);
        for (int i = 0; i < numGroups; i++)
        {
            uint groupId = Ar.Read<uint>();
            byte groupType = Ar.Read<byte>();
            var numStates = Ar.Read<ushort>();

            var states = new List<AkState>(numStates);
            for (int s = 0; s < numStates; s++)
            {
                uint stateId = Ar.Read<uint>();
                uint stateInstanceId = Ar.Read<uint>();
                states.Add(new AkState(stateId, stateInstanceId, []));
            }

            Groups.Add(new AkStateGroup(groupId, groupType, states));
        }
    }
}

public class AkStateAwareChunk
{
    public readonly List<AkStatePropertyInfo> StateProperties;
    public readonly List<AkStateGroup> Groups;

    public AkStateAwareChunk(FArchive Ar)
    {
        var statePropsCount = WwiseReader.Read7BitEncodedIntBE(Ar);
        StateProperties = new List<AkStatePropertyInfo>(statePropsCount);
        for (int i = 0; i < statePropsCount; i++)
        {
            StateProperties.Add(new AkStatePropertyInfo(Ar));
        }

        int groupCount = WwiseReader.Read7BitEncodedIntBE(Ar);
        Groups = new List<AkStateGroup>(groupCount);
        for (int g = 0; g < groupCount; g++)
        {
            uint groupId = Ar.Read<uint>();
            byte groupType = Ar.Read<byte>();

            int stateCount = WwiseReader.Read7BitEncodedIntBE(Ar);
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

            Groups.Add(new AkStateGroup(groupId, groupType, states));
        }
    }
}

public class AkStateProperty(ushort id, float value)
{
    public readonly ushort Id = id;
    public readonly float Value = value;
}

public class AkState(uint id, uint? stateInstanceId, List<AkStateProperty> properties)
{
    public readonly uint Id = id;
    public readonly uint? StateInstanceId = stateInstanceId;
    public readonly List<AkStateProperty> Properties = properties;
}

public class AkStateGroup(uint id, byte groupType, List<AkState> states)
{
    public readonly uint Id = id;
    public readonly byte GroupType = groupType;
    public readonly List<AkState> States = states;
}
