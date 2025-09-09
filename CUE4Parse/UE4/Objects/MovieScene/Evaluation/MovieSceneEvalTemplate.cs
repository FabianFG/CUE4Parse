using CUE4Parse.UE4.Assets.Objects;
using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Versions;
using CUE4Parse.Utils;
using Newtonsoft.Json;

namespace CUE4Parse.UE4.Objects.MovieScene.Evaluation;

public class FMovieSceneEvalTemplatePtr : IUStruct
{
    public string TypeName;
    public FStructFallback? Data;

    public FMovieSceneEvalTemplatePtr(FAssetArchive Ar)
    {
        TypeName = Ar.ReadFString();
        if (string.IsNullOrEmpty(TypeName)) return;

        var type = TypeName.SubstringAfterLast('.');
        Data = type switch
        {
            "MovieSceneLiveLinkSectionTemplate" => new FMovieSceneLiveLinkSectionTemplate(Ar),
            _ => new FStructFallback(Ar, type),
        };
    }
}

public class FMovieSceneLiveLinkSectionTemplate : FStructFallback
{
    public string? StaticDataTypeName;
    public FStructFallback? StaticData;

    public FMovieSceneLiveLinkSectionTemplate(FAssetArchive Ar) : base(Ar, "MovieSceneLiveLinkSectionTemplate")
    {
        if (FLiveLinkCustomVersion.Get(Ar) >= FLiveLinkCustomVersion.Type.NewLiveLinkRoleSystem)
        {
            if (Ar.ReadBoolean())
            {
                StaticDataTypeName = Ar.ReadFString();
                if (string.IsNullOrEmpty(StaticDataTypeName))
                    return;

                var type = StaticDataTypeName.SubstringAfterLast('.');
                StaticData = new FStructFallback(Ar, type);
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

                var type = StaticDataTypeName.SubstringAfterLast('.');
                StaticData = new FStructFallback(Ar, type);
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
