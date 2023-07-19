using System.Runtime.InteropServices;
using CUE4Parse.UE4.Objects.Core.Math;
using CUE4Parse.UE4.Objects.Core.Misc;

namespace CUE4Parse.UE4.Objects.MovieScene
{
    [StructLayout(LayoutKind.Sequential)]
    public readonly struct FMovieSceneFrameRange : IUStruct
    {
        public readonly TRange<FFrameNumber> Value;
    }
}
