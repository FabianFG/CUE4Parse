using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Objects.UObject;

namespace CUE4Parse.UE4.Assets.Exports.MovieScene;

public class UMovieSceneSequence : UMovieSceneSignedObject
{
    public FPackageIndex CompiledData;

    public override void Deserialize(FAssetArchive Ar, long validPos)
    {
        base.Deserialize(Ar, validPos);

        CompiledData = GetOrDefault(nameof(CompiledData), new FPackageIndex());
    }
}
