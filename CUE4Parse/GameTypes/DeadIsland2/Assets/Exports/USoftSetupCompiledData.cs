using CUE4Parse.UE4.Assets.Exports;
using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Objects.Core.Math;
using Newtonsoft.Json;

namespace CUE4Parse.GameTypes.DeadIsland2.Assets.Exports;

public class USoftSetupCompiledData : UObject
{
    public FVector4[] Particles;
    public FTransform[] Transforms;

    public override void Deserialize(FAssetArchive Ar, long validPos)
    {
        base.Deserialize(Ar, validPos);

        Ar.SkipArray<FVector4>();
        Particles = Ar.ReadArray<FVector4>();
        if (Particles.Length == 0) return;
        Transforms = Ar.ReadArray(Ar.Read<int>() / 4, () => new FTransform(new FMatrix(Ar)));
    }

    protected internal override void WriteJson(JsonWriter writer, JsonSerializer serializer)
    {
        base.WriteJson(writer, serializer);
        writer.WritePropertyName(nameof(Particles));
        serializer.Serialize(writer, Particles);
        writer.WritePropertyName(nameof(Transforms));
        serializer.Serialize(writer, Transforms);
    }
}
