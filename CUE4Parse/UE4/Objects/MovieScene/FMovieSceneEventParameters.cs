using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Objects.UObject;
using CUE4Parse.UE4.Versions;

namespace CUE4Parse.UE4.Objects.MovieScene;

public struct FMovieSceneEventParameters : IUStruct
{
    public FPackageIndex? StructPtr;
    public FSoftObjectPath? StructType;
    public byte[] StructBytes;

    public FMovieSceneEventParameters(FAssetArchive Ar)
    {
        if (FReleaseObjectVersion.Get(Ar) < FReleaseObjectVersion.Type.EventSectionParameterStringAssetRef)
        {
            StructPtr = new FPackageIndex(Ar);
        }
        else
        {
            StructType = new FSoftObjectPath(Ar);
        }

        StructBytes = Ar.ReadArray<byte>();
    }
}
