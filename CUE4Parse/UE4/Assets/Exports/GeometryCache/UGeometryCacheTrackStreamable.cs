using CUE4Parse.UE4.Assets.Readers;

namespace CUE4Parse.UE4.Assets.Exports.GeometryCache;

public class UGeometryCacheTrackStreamable : UGeometryCacheTrack
{
    public FStreamedGeometryCacheChunk[] Chunks;
    public FGeometryCacheTrackStreamableSampleInfo[] Samples;
    public FVisibilitySample[] VisibilitySamples;
    
    public override void Deserialize(FAssetArchive Ar, long validPos)
    {
        base.Deserialize(Ar, validPos);

        Chunks = Ar.ReadArray(() => new FStreamedGeometryCacheChunk(Ar));
        Samples = Ar.ReadArray(() => new FGeometryCacheTrackStreamableSampleInfo(Ar));
        VisibilitySamples = Ar.ReadArray(() => new FVisibilitySample(Ar));
    }
}