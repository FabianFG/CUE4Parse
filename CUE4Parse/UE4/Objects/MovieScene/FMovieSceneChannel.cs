using CUE4Parse.UE4.Assets.Objects;
using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Objects.Core.Misc;
using CUE4Parse.UE4.Objects.Engine.Curves;
using CUE4Parse.UE4.Versions;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace CUE4Parse.UE4.Objects.MovieScene;

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
    public readonly FStructFallback? StructFallback;

    public FMovieSceneChannel(FAssetArchive Ar)
    {
        if (FSequencerObjectVersion.Get(Ar) < FSequencerObjectVersion.Type.SerializeFloatChannelCompletely &&
                FFortniteMainBranchObjectVersion.Get(Ar) < FFortniteMainBranchObjectVersion.Type.SerializeFloatChannelShowCurve)
        {
            PreInfinityExtrap = ERichCurveExtrapolation.RCCE_None;
            PostInfinityExtrap = ERichCurveExtrapolation.RCCE_None;
            Times = [];
            Values = [];
            DefaultValue = default;
            bHasDefaultValue = false;
            TickResolution = default;
            bShowCurve = false;
            StructFallback = new FStructFallback(Ar, "MovieSceneChannel");
            return;
        }

        PreInfinityExtrap = Ar.Read<ERichCurveExtrapolation>();
        PostInfinityExtrap = Ar.Read<ERichCurveExtrapolation>();

        var SerializedElementSize = Ar.Read<int>();
        Times = Ar.ReadArray<FFrameNumber>();

        SerializedElementSize = Ar.Read<int>();
        Values = Ar.ReadArray(() => new FMovieSceneValue<T>(Ar, Ar.Read<T>()));

        DefaultValue = Ar.Read<T>();
        bHasDefaultValue = Ar.ReadBoolean();
        TickResolution = Ar.Read<FFrameRate>();
        bShowCurve = FFortniteMainBranchObjectVersion.Get(Ar) >= FFortniteMainBranchObjectVersion.Type.SerializeFloatChannelShowCurve && Ar.ReadBoolean(); // bShowCurve should still only be assigned while in editor
    }
}
