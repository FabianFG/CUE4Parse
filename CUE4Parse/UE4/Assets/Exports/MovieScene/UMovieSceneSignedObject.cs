using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Objects.Core.Misc;

namespace CUE4Parse.UE4.Assets.Exports.MovieScene;

public class UMovieSceneSignedObject : UObject
{
    public FGuid Signature;

    public override void Deserialize(FAssetArchive Ar, long validPos)
    {
        base.Deserialize(Ar, validPos);

        Signature = GetOrDefault<FGuid>(nameof(Signature));
    }
}
