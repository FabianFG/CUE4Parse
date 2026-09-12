using CUE4Parse.UE4.Assets.Exports.Nanite;
using CUE4Parse.UE4.Assets.Objects;
using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Objects.Chaos.GeometryCollection;
using CUE4Parse.UE4.Objects.Core.Math;
using CUE4Parse.UE4.Readers;
using Newtonsoft.Json;

namespace CUE4Parse.UE4.Assets.Exports.GeometryCollection;

public class FGeometryCollectionRenderData
{
    public bool bHasMeshData;
    public bool bHasNaniteData;
    public FGeometryCollectionMeshResources? MeshResources;
    public FGeometryCollectionMeshDescription? MeshDescription;
    public FNaniteResources? NaniteResources;
    public FBoxSphereBounds? PreSkinnedBounds;
    [JsonIgnore] public object? CustomData;

    public FGeometryCollectionRenderData(FAssetArchive Ar, bool bStripRenderDataOnCook, bool nanite = false)
    {
        if (Ar.Game < GAME_UE5_3)
        {
            NaniteResources = new FNaniteResources(Ar);
            return;
        }
        if (Ar.Game is GAME_MarvelRivals)
        {
            var customData = new List<(FGeometryCollectionMeshResources?, FGeometryCollectionMeshDescription?)>();
            while (Ar.Position < Ar.Length -4)
            {
                FGeometryCollectionMeshResources? mesh = null!;
                FGeometryCollectionMeshDescription? meshdesc = null!;
                var flags = Ar.Read<int>();
                if (flags == 0) continue;
                var idk0 = Ar.ReadArray(Ar.ReadBoolean);
                var inlined = Ar.ReadBoolean();
                if (nanite)
                {
                    NaniteResources = new FNaniteResources(Ar);
                    bHasNaniteData = true;
                    return;
                }
                var size = Ar.Read<int>(); // bulksize -96
                if (idk0.Length == 0 && !inlined && size is < 5)
                {
                    Ar.Position -= 4;
                    continue;
                }

                if (!inlined)
                {
                    using var bulkAr =  new FByteArchive("", new FByteBulkData(Ar)!.Data!, Ar.Versions);
                    mesh = new FGeometryCollectionMeshResources(bulkAr);
                    Ar.Position += 44; // buffers description
                }
                else
                {
                    mesh = new FGeometryCollectionMeshResources(Ar);
                }

                meshdesc = new FGeometryCollectionMeshDescription(Ar);
                customData.Add((mesh, meshdesc));
            }

            CustomData = customData;
            bHasMeshData = true;
            return;
        }

        bHasMeshData = Ar.ReadBoolean();
        bHasNaniteData = Ar.ReadBoolean();

        if (Ar.Game < GAME_UE5_7 && bStripRenderDataOnCook) return;

        if (bHasMeshData)
        {
            MeshResources = new FGeometryCollectionMeshResources(Ar);
            MeshDescription = new FGeometryCollectionMeshDescription(Ar);
        }

        if (bHasNaniteData) NaniteResources = new FNaniteResources(Ar);
        if (Ar.Game >= GAME_UE5_7) PreSkinnedBounds = new FBoxSphereBounds(Ar);

        if (Ar.Game == GAME_GearsofWarEDay && MeshResources?.StaticMeshVertexBuffer.Strides == -1)
        {
            bHasMeshData = false;
        }
    }
}
