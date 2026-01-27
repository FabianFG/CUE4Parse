using CUE4Parse.UE4.Assets.Objects;
using CUE4Parse.UE4.Assets.Readers;

namespace CUE4Parse.UE4.Assets.Exports.GeometryCache;

public struct FStreamedGeometryCacheChunk
{
    public FByteBulkData BulkData;
    public int DataSize;
    public float FirstFrame;
    public float LastFrame;
    
    public FStreamedGeometryCacheChunk(FAssetArchive Ar)
    {
        BulkData = new FByteBulkData(Ar);
        DataSize = Ar.Read<int>();
        FirstFrame = Ar.Read<float>();
        LastFrame = Ar.Read<float>();
    }
}