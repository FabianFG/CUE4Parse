using CUE4Parse.UE4.Readers;

namespace CUE4Parse.UE4.Objects.Engine.InstanceData;

public class FInstanceIdIndexMap
{
    // Bidirectional mapping to / from ID.
    public FPrimitiveInstanceId[] IndexToIdMap;
    public int[] IdToIndexMap;
    // used when the mapping is implicit (i.e., identity)
    public int NumInstances = 0;

    public FInstanceIdIndexMap(FArchive Ar)
    {
        IndexToIdMap = Ar.ReadArray<FPrimitiveInstanceId>();
        NumInstances = Ar.Read<int>();
        var maxInstanceId = Ar.Read<int>();
        IdToIndexMap = new int[maxInstanceId];
        Array.Fill(IdToIndexMap, -1);
        for (int instanceIndex = 0; instanceIndex < IndexToIdMap.Length; ++instanceIndex)
        {
            IdToIndexMap[IndexToIdMap[instanceIndex].Id] = instanceIndex;
        }
    }
}
