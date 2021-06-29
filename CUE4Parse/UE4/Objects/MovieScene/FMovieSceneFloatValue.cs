using CUE4Parse.UE4.Objects.Engine.Curves;
using System.Runtime.InteropServices;

namespace CUE4Parse.UE4.Objects.MovieScene
{
    [StructLayout(LayoutKind.Sequential)]
    public readonly struct FMovieSceneFloatValue : IUStruct
    {
        public readonly float FloatValue;
        public readonly FMovieSceneTangentData Tangent;
        public readonly ERichCurveInterpMode InterpMode;
        public readonly ERichCurveTangentMode TangentMode;
        public readonly byte PaddingByte;
    }
}
