using CUE4Parse.UE4.Assets.Objects;
using CUE4Parse.UE4.Assets.Readers;

namespace CUE4Parse.UE4.Assets.Exports.Groom;

public class FHairStrandsRootBulkData(FAssetArchive Ar)
{
    public uint RootCount = Ar.Read<uint>();
    public uint PointCount = Ar.Read<uint>();
    public FByteBulkData VertexToCurveIndexBuffer = new(Ar);
    public FMeshProjectionLOD_UE5[] MeshProjectionLODs = Ar.ReadArray(() => new FMeshProjectionLOD_UE5(Ar));
}

public class FMeshProjectionLOD_UE5
{
    public int LODIndex;
    public FByteBulkData RootTriangleIndexBuffer;
    public FByteBulkData RootTriangleBarycentricBuffer;
    public FByteBulkData RestRootTrianglePosition0Buffer;
    public FByteBulkData RestRootTrianglePosition1Buffer;
    public FByteBulkData RestRootTrianglePosition2Buffer;
    public uint SampleCount;
    public FByteBulkData? MeshInterpolationWeightsBuffer;
    public FByteBulkData? MeshSampleIndicesBuffer;
    public FByteBulkData? RestSamplePositionsBuffer;
    public uint[] ValidSectionIndices;

    public FMeshProjectionLOD_UE5(FAssetArchive Ar)
    {
        LODIndex = Ar.Read<int>();
        RootTriangleIndexBuffer = new FByteBulkData(Ar);
        RootTriangleBarycentricBuffer = new FByteBulkData(Ar);
        RestRootTrianglePosition0Buffer = new FByteBulkData(Ar);
        RestRootTrianglePosition1Buffer = new FByteBulkData(Ar);
        RestRootTrianglePosition2Buffer = new FByteBulkData(Ar);

        SampleCount = Ar.Read<uint>();
        if (SampleCount != 0)
        {
            MeshInterpolationWeightsBuffer = new FByteBulkData(Ar);
            MeshSampleIndicesBuffer = new FByteBulkData(Ar);
            RestSamplePositionsBuffer = new FByteBulkData(Ar);
        }

        ValidSectionIndices = Ar.ReadBulkArray<uint>();
    }
}