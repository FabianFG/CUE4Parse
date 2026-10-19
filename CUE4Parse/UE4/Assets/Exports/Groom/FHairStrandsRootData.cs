using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Objects.Core.Math;

namespace CUE4Parse.UE4.Assets.Exports.Groom;

public class FHairStrandsRootData(FAssetArchive Ar)
{
    public uint RootCount = Ar.Read<uint>();
    public uint[] VertexToCurveIndexBuffer = Ar.ReadArray<uint>();
    public FVector4[] RootPositionBuffer = Ar.ReadArray<FVector4>();
    public FVector4_16[] RootNormalBuffer = Ar.ReadArray(() => new FVector4_16(Ar));
    public FMeshProjectionLOD_UE4[] MeshProjectionLODs = Ar.ReadArray(() => new FMeshProjectionLOD_UE4(Ar));
}

public class FMeshProjectionLOD_UE4
{
    public int LODIndex;
    public uint[] RootTriangleIndexBuffer;
    public uint[] RootTriangleBarycentricBuffer;
    public FVector4[] RestRootTrianglePosition0Buffer;
    public FVector4[] RestRootTrianglePosition1Buffer;
    public FVector4[] RestRootTrianglePosition2Buffer;
    public uint SampleCount;
    public float[] MeshInterpolationWeightsBuffer;
    public uint[] MeshSampleIndicesBuffer;
    public FVector4[] RestSamplePositionsBuffer;

    public FMeshProjectionLOD_UE4(FAssetArchive Ar)
    {
        LODIndex = Ar.Read<int>();
        RootTriangleIndexBuffer = Ar.ReadArray<uint>();
        RootTriangleBarycentricBuffer = Ar.ReadArray<uint>();
        RestRootTrianglePosition0Buffer = Ar.ReadArray<FVector4>();
        RestRootTrianglePosition1Buffer = Ar.ReadArray<FVector4>();
        RestRootTrianglePosition2Buffer = Ar.ReadArray<FVector4>();
        SampleCount = Ar.Read<uint>();
        MeshInterpolationWeightsBuffer = Ar.ReadArray<float>();
        MeshSampleIndicesBuffer = Ar.ReadArray<uint>();
        RestSamplePositionsBuffer = Ar.ReadArray<FVector4>();
    }
}