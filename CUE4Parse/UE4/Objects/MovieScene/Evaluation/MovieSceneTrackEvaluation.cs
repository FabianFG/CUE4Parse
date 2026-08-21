using CUE4Parse.UE4.Assets.Objects;
using CUE4Parse.UE4.Assets.Readers;

namespace CUE4Parse.UE4.Objects.MovieScene.Evaluation;

public class FMovieSceneTrackImplementationPtr : IUStruct
{
    public string TypeName;
    public FStructFallback? Data;

    public FMovieSceneTrackImplementationPtr(FAssetArchive Ar)
    {
        TypeName = Ar.ReadFString();
        if (string.IsNullOrEmpty(TypeName)) return;

        Data = new FStructFallback(Ar, TypeName);
    }
}
