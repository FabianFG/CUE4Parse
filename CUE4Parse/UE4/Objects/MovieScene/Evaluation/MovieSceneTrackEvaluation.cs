using CUE4Parse.UE4.Assets.Objects;
using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.Utils;

namespace CUE4Parse.UE4.Objects.MovieScene.Evaluation;

public class FMovieSceneTrackImplementationPtr : IUStruct
{
    public string TypeName;
    public FStructFallback? Data;

    public FMovieSceneTrackImplementationPtr(FAssetArchive Ar)
    {
        TypeName = Ar.ReadFString();
        if (string.IsNullOrEmpty(TypeName)) return;

        var type = TypeName.SubstringAfterLast('.');
        Data = new FStructFallback(Ar, type);
    }
}
