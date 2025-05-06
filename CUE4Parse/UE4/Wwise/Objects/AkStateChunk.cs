using System.Collections.Generic;
using CUE4Parse.UE4.Readers;

namespace CUE4Parse.UE4.Wwise.Objects;

public class AkStateProperty
{
    public ushort ID { get; }
    public float Value { get; }

    public AkStateProperty(ushort id, float value)
    {
        ID = id;
        Value = value;
    }
}

public class AkState
{
    public uint ID { get; }
    public uint? StateInstanceID { get; }
    public List<AkStateProperty> Properties { get; }

    public AkState(uint id, uint? stateInstanceId, List<AkStateProperty> properties)
    {
        ID = id;
        StateInstanceID = stateInstanceId;
        Properties = properties;
    }
}

public class AkStateGroup
{
    public uint ID { get; }
    public byte GroupType { get; }
    public List<AkState> States { get; }

    public AkStateGroup(uint id, byte groupType, List<AkState> states)
    {
        ID = id;
        GroupType = groupType;
        States = states;
    }
}

public class AkStateChunk
{
    public int HeaderCount { get; }
    public List<AkStateGroup> Groups { get; }

    public AkStateChunk(FArchive ar)
    {
        // Read header metadata
        HeaderCount = ar.Read7BitEncodedInt();
        for (int i = 0; i < HeaderCount; i++)
        {
            ar.Read7BitEncodedInt();
            ar.Read<byte>();
            ar.Read<byte>();
        }

        // Read groups
        int groupCount = ar.Read7BitEncodedInt();
        var groups = new List<AkStateGroup>(groupCount);
        for (int g = 0; g < groupCount; g++)
        {
            uint groupId = ar.Read<uint>();
            byte groupType = ar.Read<byte>();
            int stateCount = ar.Read7BitEncodedInt();

            var states = new List<AkState>(stateCount);
            for (int s = 0; s < stateCount; s++)
            {
                uint stateId = ar.Read<uint>();
                if (WwiseVersions.WwiseVersion <= 145)
                {
                    uint stateInstanceId = ar.Read<uint>();
                    states.Add(new AkState(stateId, stateInstanceId, []));
                }
                else
                {
                    ushort propCount = ar.Read<ushort>();
                    var props = new List<AkStateProperty>(propCount);
                    for (int k = 0; k < propCount; k++)
                    {
                        ushort propId = ar.Read<ushort>();
                        float value = ar.Read<float>();
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
