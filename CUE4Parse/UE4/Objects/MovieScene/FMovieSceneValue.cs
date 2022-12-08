using System.Runtime.InteropServices;
using CUE4Parse.UE4.Objects.Engine.Curves;

namespace CUE4Parse.UE4.Objects.MovieScene
{
    [StructLayout(LayoutKind.Sequential)]
    public readonly struct FMovieSceneValue<T> : IUStruct
    {
        public readonly T Value;
        public readonly FMovieSceneTangentData Tangent;
        public readonly ERichCurveInterpMode InterpMode;
        public readonly ERichCurveTangentMode TangentMode;
        public readonly byte PaddingByte;
    }
}
