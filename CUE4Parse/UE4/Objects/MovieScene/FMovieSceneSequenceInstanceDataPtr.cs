using System.Runtime.InteropServices;
using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Objects.UObject;

namespace CUE4Parse.UE4.Objects.MovieScene;

public readonly struct FMovieSceneSequenceInstanceDataPtr(FAssetArchive Ar) : IUStruct
{
    public readonly FPackageIndex Value = new FPackageIndex(Ar);
}
