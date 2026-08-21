using CUE4Parse.UE4.Assets.Objects;
using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Versions;
using CUE4Parse.MappingsProvider;
using Newtonsoft.Json;

namespace CUE4Parse.UE4.Objects.MovieScene.Evaluation;

public class FMovieSceneEvalTemplatePtr : IUStruct
{
    private const string LiveLinkTemplateIdentifier =
        "/Script/LiveLinkMovieScene.MovieSceneLiveLinkSectionTemplate";

    public string TypeName;
    public FStructFallback? Data;

    public FMovieSceneEvalTemplatePtr(FAssetArchive Ar)
    {
        TypeName = Ar.ReadFString();
        if (string.IsNullOrEmpty(TypeName)) return;

        var isLegacyLiveLinkName = !TypeMappings.IsFullTypeIdentifier(TypeName) &&
                                   TypeName.Equals("MovieSceneLiveLinkSectionTemplate", StringComparison.OrdinalIgnoreCase);
        Data = TypeName.Equals(LiveLinkTemplateIdentifier, StringComparison.OrdinalIgnoreCase) || isLegacyLiveLinkName
            ? new FMovieSceneLiveLinkSectionTemplate(Ar)
            : new FStructFallback(Ar, TypeName);
    }
}

public class FMovieSceneLiveLinkSectionTemplate : FStructFallback
{
    private const string TypeIdentifier = "/Script/LiveLinkMovieScene.MovieSceneLiveLinkSectionTemplate";
    public string? StaticDataTypeName;
    public FStructFallback? StaticData;

    public FMovieSceneLiveLinkSectionTemplate(FAssetArchive Ar) : base(Ar, TypeIdentifier)
    {
        if (FLiveLinkCustomVersion.Get(Ar) >= FLiveLinkCustomVersion.Type.NewLiveLinkRoleSystem)
        {
            if (Ar.ReadBoolean())
            {
                StaticDataTypeName = Ar.ReadFString();
                if (string.IsNullOrEmpty(StaticDataTypeName))
                    return;

                StaticData = new FStructFallback(Ar, StaticDataTypeName);
            }
        }
    }
}

public class UMovieSceneLiveLinkSection : Assets.Exports.UObject
{
    public string? StaticDataTypeName;
    public FStructFallback? StaticData;

    public override void Deserialize(FAssetArchive Ar, long validPos)
    {
        base.Deserialize(Ar, validPos);
        if (FLiveLinkCustomVersion.Get(Ar) >= FLiveLinkCustomVersion.Type.NewLiveLinkRoleSystem)
        {
            if (Ar.ReadBoolean())
            {
                StaticDataTypeName = Ar.ReadFString();
                if (string.IsNullOrEmpty(StaticDataTypeName))
                    return;

                StaticData = new FStructFallback(Ar, StaticDataTypeName);
            }
        }
    }

    protected internal override void WriteJson(JsonWriter writer, JsonSerializer serializer)
    {
        base.WriteJson(writer, serializer);
        if (StaticData is null) return;

        writer.WritePropertyName("StaticData");
        serializer.Serialize(writer, StaticData);
    }
}
