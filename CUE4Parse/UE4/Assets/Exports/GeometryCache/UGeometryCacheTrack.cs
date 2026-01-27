using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Objects.Core.Math;
using CUE4Parse.UE4.Versions;
using Newtonsoft.Json;

namespace CUE4Parse.UE4.Assets.Exports.GeometryCache;

public class UGeometryCacheTrack : UObject
{
    public FMatrix[] MatrixSamples;
    public float[] MatrixSampleTimes;
    public uint NumMaterials;
    
    public override void Deserialize(FAssetArchive Ar, long validPos)
    {
        if (FAnimPhysObjectVersion.Get(Ar) >= FAnimPhysObjectVersion.Type.GeometryCacheAssetDeprecation)
        {
            base.Deserialize(Ar, validPos);
        }

        MatrixSamples = Ar.ReadArray(() => new FMatrix(Ar));
        MatrixSampleTimes = Ar.ReadArray<float>();
        NumMaterials = Ar.Read<uint>();
    }

    protected internal override void WriteJson(JsonWriter writer, JsonSerializer serializer)
    {
        base.WriteJson(writer, serializer);
        
        writer.WritePropertyName(nameof(MatrixSamples));
        serializer.Serialize(writer, MatrixSamples);
        
        writer.WritePropertyName(nameof(MatrixSampleTimes));
        serializer.Serialize(writer, MatrixSampleTimes);
        
        writer.WritePropertyName(nameof(NumMaterials));
        serializer.Serialize(writer, NumMaterials);
    }
}