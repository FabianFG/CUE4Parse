using CUE4Parse.UE4.Assets.Exports.Nanite;
using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Objects.Chaos.GeometryCollection;
using CUE4Parse.UE4.Objects.Core.Math;

namespace CUE4Parse.UE4.Assets.Exports.GeometryCollection;

public class FGeometryCollectionRenderData
{
    public bool bHasMeshData;
    public bool bHasNaniteData;
    public FGeometryCollectionMeshResources? MeshResources;
    public FGeometryCollectionMeshDescription? MeshDescription;
    public FNaniteResources? NaniteResources;
    public FBoxSphereBounds? PreSkinnedBounds;

    public FGeometryCollectionRenderData(FAssetArchive Ar)
    {
        bHasMeshData = Ar.ReadBoolean();
        bHasNaniteData = Ar.ReadBoolean();

        if (bHasMeshData)
        {
            MeshResources = new FGeometryCollectionMeshResources(Ar);
            MeshDescription = new FGeometryCollectionMeshDescription(Ar);
        }

        if (bHasNaniteData) NaniteResources = new FNaniteResources(Ar);
        if (Ar.Game >= GAME_UE5_7) PreSkinnedBounds = new FBoxSphereBounds(Ar);
    }
}
