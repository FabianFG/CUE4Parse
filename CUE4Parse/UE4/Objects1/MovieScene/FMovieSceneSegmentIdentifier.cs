using System.Runtime.InteropServices;

namespace CUE4Parse.UE4.Objects.MovieScene
{
    [StructLayout(LayoutKind.Sequential)]
    public readonly struct FMovieSceneSegmentIdentifier : IUStruct
    {
        public readonly int IdentifierIndex;
    }
}
