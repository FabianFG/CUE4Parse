using System.Collections.Generic;
using CUE4Parse.UE4.Readers;

namespace CUE4Parse.UE4.Wwise.Objects
{
    public struct AkStateProperty
    {
        public ushort ID;
        public float Value;
    }

    public struct AkState
    {
        public uint ID;
        public List<AkStateProperty> Properties;
    }

    public struct AkStateGroup
    {
        public uint ID;
        public byte GroupType;
        public List<AkState> States;
    }

    public static class AkStateChunk
    {
        public static List<AkStateGroup> ReadStateChunk(this FArchive Ar)
        {
            int propCount = Ar.Read7BitEncodedInt();
            for (int i = 0; i < propCount; i++)
            {
                Ar.Read7BitEncodedInt();
                Ar.Read<byte>();
                Ar.Read<byte>();
            }

            var stateGroups = new List<AkStateGroup>();

            int groupCount = Ar.Read7BitEncodedInt();
            for (int g = 0; g < groupCount; g++)
            {
                uint groupId = Ar.Read<uint>();
                byte groupType = Ar.Read<byte>();

                int stateCount = Ar.Read7BitEncodedInt();
                var states = new List<AkState>(stateCount);

                for (int s = 0; s < stateCount; s++)
                {
                    uint stateId = Ar.Read<uint>();
                    ushort propCountInState = Ar.Read<ushort>();

                    var props = new List<AkStateProperty>(propCountInState);
                    for (int k = 0; k < propCountInState; k++)
                    {
                        ushort propId = Ar.Read<ushort>();
                        float value = Ar.Read<float>();
                        props.Add(new AkStateProperty { ID = propId, Value = value });
                    }

                    states.Add(new AkState
                    {
                        ID = stateId,
                        Properties = props
                    });
                }

                stateGroups.Add(new AkStateGroup
                {
                    ID = groupId,
                    GroupType = groupType,
                    States = states
                });
            }

            return stateGroups;
        }
    }
}
