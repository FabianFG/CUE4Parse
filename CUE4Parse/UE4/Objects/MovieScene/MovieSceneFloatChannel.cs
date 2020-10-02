using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Objects.Core.Misc;
using CUE4Parse.UE4.Objects.Engine.Curves;
using System.Runtime.InteropServices;

namespace CUE4Parse.UE4.Objects.MovieScene
{
    public class MovieSceneFloatChannel : IUStruct
    {
        public readonly ERichCurveExtrapolation PreInfinityExtrap;
        public readonly ERichCurveExtrapolation PostInfinityExtrap;
        public readonly FFrameNumber[]? Times;
        public readonly FMovieSceneFloatValue[]? Values;
        public readonly float DefaultValue;
        public readonly bool bHasDefaultValue;
        public readonly FFrameRate TickResolution;

        public MovieSceneFloatChannel(FAssetArchive Ar)
        {
            PreInfinityExtrap = Ar.Read<ERichCurveExtrapolation>();
            PostInfinityExtrap = Ar.Read<ERichCurveExtrapolation>();

            int CurrentSerializedElementSize = Marshal.SizeOf(typeof(FFrameNumber));
            int SerializedElementSize = Ar.Read<int>();
            if (SerializedElementSize != CurrentSerializedElementSize)
            {
                Times = Ar.ReadArray<FFrameNumber>();
            }
            CurrentSerializedElementSize = Marshal.SizeOf(typeof(FMovieSceneFloatValue));
            SerializedElementSize = Ar.Read<int>();
            if (SerializedElementSize != CurrentSerializedElementSize)
            {
                Values = Ar.ReadArray<FMovieSceneFloatValue>();
            }

            DefaultValue = Ar.Read<float>();
            bHasDefaultValue = Ar.ReadBoolean();
            TickResolution = Ar.Read<FFrameRate>();
        }
    }
}
