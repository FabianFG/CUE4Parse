using System;
using System.Runtime.CompilerServices;
using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Objects.Core.Misc;
using CUE4Parse.UE4.Objects.Engine.Curves;
using CUE4Parse.UE4.Versions;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace CUE4Parse.UE4.Objects.MovieScene
{
    public readonly struct FMovieSceneChannel<T> : IUStruct
    {
        [JsonConverter(typeof(StringEnumConverter))]
        public readonly ERichCurveExtrapolation PreInfinityExtrap;
        [JsonConverter(typeof(StringEnumConverter))]
        public readonly ERichCurveExtrapolation PostInfinityExtrap;
        public readonly FFrameNumber[] Times;
        public readonly FMovieSceneValue<T>[] Values;
        public readonly T? DefaultValue;
        public readonly bool bHasDefaultValue; // 4 bytes
        public readonly FFrameRate TickResolution;
        public readonly bool bShowCurve;

        public FMovieSceneChannel(FAssetArchive Ar)
        {
            if (FSequencerObjectVersion.Get(Ar) < FSequencerObjectVersion.Type.SerializeFloatChannelCompletely) // && FFortniteMainBranchObjectVersion.Get(Ar) < FFortniteMainBranchObjectVersion.Type.SerializeFloatChannelShowCurve
            {
                PreInfinityExtrap = ERichCurveExtrapolation.RCCE_None;
                PostInfinityExtrap = ERichCurveExtrapolation.RCCE_None;
                Times = Array.Empty<FFrameNumber>();
                Values = Array.Empty<FMovieSceneValue<T>>();
                DefaultValue = default;
                bHasDefaultValue = false;
                TickResolution = default;
                bShowCurve = false;
                return;
            }

            PreInfinityExtrap = Ar.Read<ERichCurveExtrapolation>();
            PostInfinityExtrap = Ar.Read<ERichCurveExtrapolation>();

            var CurrentSerializedElementSize = Unsafe.SizeOf<FFrameNumber>();
            var SerializedElementSize = Ar.Read<int>();

            if (SerializedElementSize == CurrentSerializedElementSize)
            {
                Times = Ar.ReadArray<FFrameNumber>();
            }
            else
            {
                var ArrayNum = Ar.Read<int>();

                if (ArrayNum > 0)
                {
                    var padding = SerializedElementSize - CurrentSerializedElementSize;
                    Times = new FFrameNumber[ArrayNum];

                    for (var i = 0; i < ArrayNum; i++)
                    {
                        Ar.Position += padding;
                        Times[i] = Ar.Read<FFrameNumber>();
                        //Ar.Position += padding; TODO check this
                    }
                }
                else
                {
                    Times = Array.Empty<FFrameNumber>();
                }
            }

            CurrentSerializedElementSize = Unsafe.SizeOf<FMovieSceneValue<T>>();
            SerializedElementSize = Ar.Read<int>();

            if (SerializedElementSize == CurrentSerializedElementSize)
            {
                Values = Ar.ReadArray<FMovieSceneValue<T>>();
            }
            else
            {
                var ArrayNum = Ar.Read<int>();

                if (ArrayNum > 0)
                {
                    var padding = SerializedElementSize - CurrentSerializedElementSize;
                    Values = new FMovieSceneValue<T>[ArrayNum];

                    for (var i = 0; i < ArrayNum; i++)
                    {
                        Ar.Position += padding;
                        Values[i] = Ar.Read<FMovieSceneValue<T>>();
                        //Ar.Position += padding; TODO check this
                    }
                }
                else
                {
                    Values = Array.Empty<FMovieSceneValue<T>>();
                }
            }

            DefaultValue = Ar.Read<T>();
            bHasDefaultValue = Ar.ReadBoolean();
            TickResolution = Ar.Read<FFrameRate>();
            bShowCurve = FFortniteMainBranchObjectVersion.Get(Ar) >= FFortniteMainBranchObjectVersion.Type.SerializeFloatChannelShowCurve && Ar.ReadBoolean(); // bShowCurve should still only be assigned while in editor
        }
    }
}
