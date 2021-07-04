using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Objects.Engine;
using CUE4Parse.UE4.Versions;

namespace CUE4Parse.UE4.Assets.Exports.SkeletalMesh
{
    public class FSkeletalMeshVertexClothBuffer
    {
        public readonly ulong[]? ClothIndexMapping;
        
        public FSkeletalMeshVertexClothBuffer(FAssetArchive Ar)
        {
            var stripDataFlags = new FStripDataFlags(Ar, (int)UE4Version.VER_UE4_STATIC_SKELETAL_MESH_SERIALIZATION_FIX);
            if (stripDataFlags.IsDataStrippedForServer()) return;
            
            Ar.SkipBulkArrayData();
            if (FSkeletalMeshCustomVersion.Get(Ar) >= FSkeletalMeshCustomVersion.Type.CompactClothVertexBuffer)
            {
                ClothIndexMapping = Ar.ReadArray(Ar.Read<ulong>);
            }
        }
    }
}