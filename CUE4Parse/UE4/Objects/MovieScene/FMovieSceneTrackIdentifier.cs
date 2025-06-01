using CUE4Parse.UE4.Assets.Objects;
using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Versions;

namespace CUE4Parse.UE4.Objects.MovieScene;

public struct FMovieSceneTrackIdentifier : IUStruct
{
    public uint Value;

    public FMovieSceneTrackIdentifier(FAssetArchive Ar)
    {
        if (FEditorObjectVersion.Get(Ar) < FEditorObjectVersion.Type.MovieSceneMetaDataSerialization)
        {
            var FallbackStruct = new FStructFallback(Ar);
            Value = FallbackStruct.GetOrDefault<uint>("Value");
        }
        else
        {
            Value = Ar.Read<uint>();
        }
    }
}

public readonly struct FMovieSceneTrackIdentifiers(FAssetArchive Ar) : IUStruct
{
    public readonly FMovieSceneTrackIdentifiers[] Data = Ar.ReadArray(() => new FMovieSceneTrackIdentifiers(Ar));
}
