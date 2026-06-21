using CUE4Parse.UE4.Assets.Exports.Nanite;
using CUE4Parse.UE4.Assets.Exports.StaticMesh;
using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Objects.Core.Math;
using CUE4Parse.UE4.Objects.Meshes;
using CUE4Parse.UE4.Versions;

namespace CUE4Parse.UE4.Assets.Exports.GeometryCollection
{
    public class FBoneMapVertexBuffer
    {
        public int NumVertices;
        public ushort[] BoneMap;
        
        public FBoneMapVertexBuffer(FAssetArchive Ar)
        {
            NumVertices = Ar.Read<int>();
            BoneMap = Ar.ReadBulkArray<ushort>();
        } 
    }

    public class FGeometryCollectionMeshResources
    {
        FRawStaticIndexBuffer IndexBuffer;
        FPositionVertexBuffer PositionVertexBuffer;
        FStaticMeshVertexBuffer StaticMeshVertexBuffer;
        FColorVertexBuffer ColorVertexBuffer;
        FBoneMapVertexBuffer BoneMapVertexBuffer;

        public FGeometryCollectionMeshResources(FAssetArchive Ar)
        {
            IndexBuffer = new FRawStaticIndexBuffer(Ar);
            PositionVertexBuffer = new FPositionVertexBuffer(Ar);
            StaticMeshVertexBuffer = new FStaticMeshVertexBuffer(Ar);
            ColorVertexBuffer = new FColorVertexBuffer(Ar);
            BoneMapVertexBuffer = new FBoneMapVertexBuffer(Ar);
        }
    }

    public struct FGeometryCollectionMeshElement
    {
        public short TransformIndex;
        public byte MaterialIndex;
        public byte bIsInternal;
        public uint TriangleStart;
        public uint TriangleCount;
        public uint VertexStart;
        public uint VertexEnd;
    };
    
    public class FGeometryCollectionMeshDescription
    {
        public uint NumVertices;
        public uint NumTriangles;
        public FBoxSphereBounds PreSkinnedBounds;
        public FGeometryCollectionMeshElement[] Sections;
        public FGeometryCollectionMeshElement[] SectionsNoInternal;
        public FGeometryCollectionMeshElement[] SubSections;

        public FGeometryCollectionMeshDescription(FAssetArchive Ar)
        {
            NumVertices = Ar.Read<uint>();
            NumTriangles = Ar.Read<uint>();
            PreSkinnedBounds = new FBoxSphereBounds(Ar);
            Sections = Ar.ReadArray<FGeometryCollectionMeshElement>();
            SectionsNoInternal = Ar.ReadArray<FGeometryCollectionMeshElement>();
            SubSections = Ar.ReadArray<FGeometryCollectionMeshElement>();
        }
    }
    
    
    public class FGeometryCollectionNaniteData
    {
        public FNaniteResources NaniteResources { get; private set; }
        public FGeometryCollectionMeshResources MeshResources;
        public FGeometryCollectionMeshDescription MeshDescription;

        public FGeometryCollectionNaniteData(FAssetArchive Ar)
        {
            var bHasMeshData = Ar.ReadBoolean();
            var bHasNaniteData  = Ar.ReadBoolean();

            if (Ar.Game == EGame.GAME_MarvelRivals)
            {
                (bHasMeshData, bHasNaniteData) = (bHasNaniteData, bHasMeshData); // maybe?
                var something = Ar.Read<int>(); // ?
            }
            
            if (bHasMeshData)
            {
                MeshResources = new FGeometryCollectionMeshResources(Ar);
                MeshDescription  = new FGeometryCollectionMeshDescription(Ar);
            }

            if (bHasNaniteData)
                NaniteResources = new FNaniteResources(Ar);
        }
    }
}
