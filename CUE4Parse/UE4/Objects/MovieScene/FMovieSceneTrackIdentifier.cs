using System.Runtime.InteropServices;

namespace CUE4Parse.UE4.Objects.MovieScene;

[StructLayout(LayoutKind.Sequential)]
public readonly struct FMovieSceneTrackIdentifier : IUStruct
{
    public readonly uint Value;
}
