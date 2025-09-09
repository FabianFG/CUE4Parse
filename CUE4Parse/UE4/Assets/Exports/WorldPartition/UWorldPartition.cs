using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Objects.UObject;
using CUE4Parse.UE4.Versions;

namespace CUE4Parse.UE4.Assets.Exports.WorldPartition;

public class UWorldPartition : UObject
{
    public bool bCooked;
    public FPackageIndex? RuntimeHash;
    public FPackageIndex? StreamingPolicy;

    public override void Deserialize(FAssetArchive Ar, long validPos)
    {
        base.Deserialize(Ar, validPos);
        RuntimeHash = GetOrDefault<FPackageIndex>(nameof(RuntimeHash));

        if (FUE5MainStreamObjectVersion.Get(Ar) >= FUE5MainStreamObjectVersion.Type.WorldPartitionSerializeStreamingPolicyOnCook)
        {
            bCooked = Ar.ReadBoolean();
            if (bCooked)
                StreamingPolicy = new FPackageIndex(Ar);
        }
    }
}
