using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Objects.Meshes;
using CUE4Parse.UE4.Objects.UObject;
using CUE4Parse.UE4.Versions;

namespace CUE4Parse.UE4.Assets.Exports.StaticMesh
{
    public class FStaticMaterial
    {
        public ResolvedObject? MaterialInterface; // UMaterialInterface
        public FName MaterialSlotName;
        public FName ImportedMaterialSlotName;
        public FMeshUVChannelInfo? UVChannelData;

        public FStaticMaterial(FAssetArchive Ar)
        {
            MaterialInterface = new FPackageIndex(Ar).ResolvedObject;
            MaterialSlotName = Ar.ReadFName();
            if (FRenderingObjectVersion.Get(Ar) >= FRenderingObjectVersion.Type.TextureStreamingMeshUVChannelData)
                UVChannelData = new FMeshUVChannelInfo(Ar);
        }
    }
}
