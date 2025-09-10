using CUE4Parse.UE4.Assets.Exports.SkeletalMesh;
using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Versions;

namespace CUE4Parse.UE4.Assets.Exports.ApexDestruction;

public class UDestructibleMesh : USkeletalMesh
{
    public byte[] NameBuffer;
    public byte[] NxDestructibleAssetBuffer;
    public byte[] CollisionDataCacheBuffer;

    public override void Deserialize(FAssetArchive Ar, long validPos)
    {
        base.Deserialize(Ar, validPos);
        NameBuffer = Ar.ReadArray<byte>();
        NxDestructibleAssetBuffer = Ar.ReadArray<byte>();
        CollisionDataCacheBuffer = [];
        if (FFrameworkObjectVersion.Get(Ar) >= FFrameworkObjectVersion.Type.CacheDestructibleOverlaps)
        {
            CollisionDataCacheBuffer = Ar.ReadArray<byte>();
        }
    }
}
