using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Objects.Core.Math;

namespace CUE4Parse.UE4.Assets.Exports.GeometryCache;

public class FGeometryCacheTrackStreamableSampleInfo : FGeometryCacheTrackSampleInfo
{
    public FGeometryCacheTrackStreamableSampleInfo(FAssetArchive Ar)
    {
        SampleTime = Ar.Read<float>();
        BoundingBox = new FBox(Ar);
        NumVertices = Ar.Read<int>();
        NumIndices = Ar.Read<int>();
    }
}