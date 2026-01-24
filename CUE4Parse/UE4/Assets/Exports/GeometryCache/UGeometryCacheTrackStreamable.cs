using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Objects.UObject;
using Newtonsoft.Json;

namespace CUE4Parse.UE4.Assets.Exports.GeometryCache;

public class UGeometryCacheTrackStreamable : UGeometryCacheTrack
{
    public FPackageIndex Codec;
    public FStreamedGeometryCacheChunk[] Chunks;
    public FGeometryCacheTrackStreamableSampleInfo[] Samples;
    public FVisibilitySample[] VisibilitySamples;
    
    public override void Deserialize(FAssetArchive Ar, long validPos)
    {
        base.Deserialize(Ar, validPos);

        Codec = GetOrDefault(nameof(Codec), new FPackageIndex());
        
        Chunks = Ar.ReadArray(() => new FStreamedGeometryCacheChunk(Ar));
        Samples = Ar.ReadArray(() => new FGeometryCacheTrackStreamableSampleInfo(Ar));
        VisibilitySamples = Ar.ReadArray(() => new FVisibilitySample(Ar));
    }

    protected internal override void WriteJson(JsonWriter writer, JsonSerializer serializer)
    {
        base.WriteJson(writer, serializer);
        
        writer.WritePropertyName(nameof(Chunks));
        serializer.Serialize(writer, Chunks);
        
        writer.WritePropertyName(nameof(Samples));
        serializer.Serialize(writer, Samples);
        
        writer.WritePropertyName(nameof(VisibilitySamples));
        serializer.Serialize(writer, VisibilitySamples);
    }
}