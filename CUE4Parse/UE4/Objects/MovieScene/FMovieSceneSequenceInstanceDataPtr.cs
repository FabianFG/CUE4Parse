using CUE4Parse.UE4.Assets.Objects;
using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.Utils;

namespace CUE4Parse.UE4.Objects.MovieScene;

public class FMovieSceneSequenceInstanceDataPtr : IUStruct
{
    public string TypeName;
    public FStructFallback? Data;

    public FMovieSceneSequenceInstanceDataPtr()
    {
        TypeName = string.Empty;
    }
    public FMovieSceneSequenceInstanceDataPtr(FAssetArchive Ar)
    {
        TypeName = Ar.ReadFString();
        if (string.IsNullOrEmpty(TypeName)) return;

        var type = TypeName.SubstringAfterLast('.');
        Data = new FStructFallback(Ar, type);
    }
}
