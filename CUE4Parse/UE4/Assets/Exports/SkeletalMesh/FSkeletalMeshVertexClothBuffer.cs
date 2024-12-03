using CUE4Parse.UE4.Objects.Engine;
using CUE4Parse.UE4.Readers;
using CUE4Parse.UE4.Versions;

namespace CUE4Parse.UE4.Assets.Exports.SkeletalMesh;

public class FSkeletalMeshVertexClothBuffer
{
    public readonly ulong[]? ClothIndexMapping;

    public FSkeletalMeshVertexClothBuffer(FArchive Ar)
    {
        var stripDataFlags = new FStripDataFlags(Ar, FPackageFileVersion.CreateUE4Version(EUnrealEngineObjectUE4Version.STATIC_SKELETAL_MESH_SERIALIZATION_FIX));
        if (stripDataFlags.IsAudioVisualDataStripped()) return;

        Ar.SkipBulkArrayData();
        if (FSkeletalMeshCustomVersion.Get(Ar) >= FSkeletalMeshCustomVersion.Type.CompactClothVertexBuffer)
        {
            ClothIndexMapping = Ar.ReadArray<ulong>();
            if (FUE5ReleaseStreamObjectVersion.Get(Ar) >= FUE5ReleaseStreamObjectVersion.Type.AddClothMappingLODBias)
            {
                Ar.Position += ClothIndexMapping.Length * 4;
            }
        }
    }
}
