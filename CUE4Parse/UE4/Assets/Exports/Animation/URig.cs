using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Versions;
using Newtonsoft.Json;

namespace CUE4Parse.UE4.Assets.Exports.Animation;

public class URig : UObject
{
    public FReferenceSkeleton? SourceSkeleton;

    public override void Deserialize(FAssetArchive Ar, long validPos)
    {
        base.Deserialize(Ar, validPos);
        if (FFrameworkObjectVersion.Get(Ar) >= FFrameworkObjectVersion.Type.AddSourceReferenceSkeletonToRig)
        {
            SourceSkeleton = new FReferenceSkeleton(Ar);
        }
    }

    protected internal override void WriteJson(JsonWriter writer, JsonSerializer serializer)
    {
        base.WriteJson(writer, serializer);
        if (SourceSkeleton != null)
        {
            writer.WritePropertyName(nameof(SourceSkeleton));
            serializer.Serialize(writer, SourceSkeleton);
        }
    }
}
