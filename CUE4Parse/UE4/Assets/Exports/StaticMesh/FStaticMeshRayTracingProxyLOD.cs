using CUE4Parse.UE4.Assets.Objects;
using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Objects.Meshes;
using CUE4Parse.UE4.Readers;
using CUE4Parse.UE4.Versions;

namespace CUE4Parse.UE4.Assets.Exports.StaticMesh;

public class FStaticMeshRayTracingProxyLOD
{
    public bool bBuffersInlined = false;
    
    public bool bOwnsBuffers;
    public bool bOwnsRayTracingGeometry;
    
    public FStaticMeshSection[] Sections;
    public FPositionVertexBuffer PositionVertexBuffer;
    public FStaticMeshVertexBuffer VertexBuffer;
    public FColorVertexBuffer ColorVertexBuffer;
    public FRawStaticIndexBuffer IndexBuffer;

    public FByteBulkData StreamableData;
    
    public FStaticMeshRayTracingProxyLOD(FAssetArchive Ar)
    {
        bOwnsBuffers = Ar.ReadBoolean();

        if (bOwnsBuffers)
        {
            Sections = Ar.ReadArray(() => new FStaticMeshSection(Ar));
        }

        bOwnsRayTracingGeometry = Ar.ReadBoolean();


        if (bBuffersInlined) // always false ???
        {
            SerializeBuffers(Ar);
        }
        else
        {
            StreamableData = new FByteBulkData(Ar);
        }
    }

    public void SerializeBuffers(FAssetArchive Ar)
    {
        if (bOwnsBuffers)
        {
            PositionVertexBuffer = new FPositionVertexBuffer(Ar);
            VertexBuffer = new FStaticMeshVertexBuffer(Ar);
            ColorVertexBuffer = new FColorVertexBuffer(Ar);

            IndexBuffer = new FRawStaticIndexBuffer(Ar);
        }

        if (bOwnsRayTracingGeometry)
        {
            var rayTracingBulkData = Ar.ReadBulkArray<byte>();
        }

    }
}