using CUE4Parse.UE4.Assets.Exports.Material;
using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Objects.Meshes;
using CUE4Parse.UE4.Objects.UObject;
using CUE4Parse.UE4.Versions;
using System;

namespace CUE4Parse.UE4.Assets.Exports.StaticMesh
{
    public class FStaticMaterial
    {
        public Lazy<UMaterialInterface?> MaterialInterface;
        public FName MaterialSlotName;
        public FName ImportedMaterialSlotName;
        public FMeshUVChannelInfo? UVChannelData;

        public FStaticMaterial(FAssetArchive Ar)
        {
            MaterialInterface = Ar.ReadObject<UMaterialInterface>();
            MaterialSlotName = Ar.ReadFName();
            if (FRenderingObjectVersion.Get(Ar) >= FRenderingObjectVersion.Type.TextureStreamingMeshUVChannelData)
                UVChannelData = new FMeshUVChannelInfo(Ar);
        }
    }
}
