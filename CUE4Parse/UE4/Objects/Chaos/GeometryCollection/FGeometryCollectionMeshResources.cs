using CUE4Parse.UE4.Assets.Exports.StaticMesh;
using CUE4Parse.UE4.Objects.Meshes;
using CUE4Parse.UE4.Readers;
using Newtonsoft.Json;

namespace CUE4Parse.UE4.Objects.Chaos.GeometryCollection;

public class FGeometryCollectionMeshResources
{
    [JsonIgnore] public FRawStaticIndexBuffer IndexBuffer;
    public FPositionVertexBuffer PositionVertexBuffer;
    public FStaticMeshVertexBuffer StaticMeshVertexBuffer;
    public FColorVertexBuffer ColorVertexBuffer;
    public FBoneMapVertexBuffer BoneMapVertexBuffer;

    public FGeometryCollectionMeshResources(FArchive Ar)
    {
        IndexBuffer = new FRawStaticIndexBuffer(Ar);
        PositionVertexBuffer = new FPositionVertexBuffer(Ar);
        StaticMeshVertexBuffer = new FStaticMeshVertexBuffer(Ar);
        ColorVertexBuffer = new FColorVertexBuffer(Ar);
        BoneMapVertexBuffer = new FBoneMapVertexBuffer(Ar);
    }
}
