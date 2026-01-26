using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Objects.Core.Math;

namespace CUE4Parse.UE4.Assets.Exports.GeometryCache;

public class FGeometryCacheTrackSampleInfo
{
    public float SampleTime;
    public FBox BoundingBox;
    public int NumVertices;
    public int NumIndices;
}