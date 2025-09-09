using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Objects.Core.Math;
using Newtonsoft.Json;

namespace CUE4Parse.UE4.Objects.Particles;

public class USubUVAnimation : Assets.Exports.UObject
{
    public FVector2D[]? BoundingGeometry;

    public override void Deserialize(FAssetArchive Ar, long validPos)
    {
        base.Deserialize(Ar, validPos);

        if (Ar.ReadBoolean())
            BoundingGeometry = Ar.ReadArray<FVector2D>();
    }

    protected internal override void WriteJson(JsonWriter writer, JsonSerializer serializer)
    {
        base.WriteJson(writer, serializer);

        if (BoundingGeometry is not { Length: > 0 }) return;

        writer.WritePropertyName("BoundingGeometry");
        serializer.Serialize(writer, BoundingGeometry);
    }
}
