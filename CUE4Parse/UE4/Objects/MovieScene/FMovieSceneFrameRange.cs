using CUE4Parse.UE4.Objects.Core.Math;
using CUE4Parse.UE4.Objects.Core.Misc;
using System.Runtime.InteropServices;

namespace CUE4Parse.UE4.Objects.MovieScene
{
    [StructLayout(LayoutKind.Sequential)]
    public readonly struct FMovieSceneFrameRange : IUStruct
    {
        public readonly TRange<FFrameNumber> FrameRangeValue;
    }
}
