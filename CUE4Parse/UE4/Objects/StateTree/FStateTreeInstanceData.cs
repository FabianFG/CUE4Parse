using CUE4Parse.UE4.Assets.Objects;
using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Versions;

namespace CUE4Parse.UE4.Objects.StateTree;

public class FStateTreeInstanceData : IUStruct
{
    public FStructFallback? Data;

    public FStateTreeInstanceData(FAssetArchive Ar)
    {
        if (FStateTreeInstanceStorageCustomVersion.Get(Ar) >= FStateTreeInstanceStorageCustomVersion.Type.AddedCustomSerialization)
        {
            Data = new FStructFallback(Ar, "StateTreeInstanceStorage");
        }
        else
        {
            Data = new FStructFallback(Ar, "StateTreeInstanceData");
        }
    }
}
