using CUE4Parse.UE4.Assets.Exports.MovieScene;
using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Objects.UObject;

namespace CUE4Parse.UE4.Assets.Exports.LevelSequence;

public class ULevelSequence : UMovieSceneSequence
{
    public FPackageIndex MovieScene;

    public override void Deserialize(FAssetArchive Ar, long validPos)
    {
        base.Deserialize(Ar, validPos);

        MovieScene = GetOrDefault(nameof(MovieScene), new FPackageIndex());
    }
}
