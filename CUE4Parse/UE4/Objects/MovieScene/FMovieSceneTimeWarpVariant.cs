using System;
using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Objects.Core.Misc;
using CUE4Parse.UE4.Objects.UObject;
using Newtonsoft.Json;

namespace CUE4Parse.UE4.Objects.MovieScene;

public enum EMovieSceneTimeWarpType : byte
{
    FixedPlayRate  = 0x0,   // FMovieSceneNumericVariant is a fixed double
    Custom         = 0x1,   // PAYLOAD_Bits is a UMovieSceneTimeWarpGetter* - matches FMovieSceneNumericVariant::TYPE_CustomPtr - 1
    FixedTime      = 0x2,   // PAYLOAD_Bits is a FMovieSceneTimeWarpFixedFrame (explicitly fixed time or zero timescale)
    FrameRate      = 0x3,   // PAYLOAD_Bits is a FMovieSceneTimeWarpFrameRate defining a frame rate from outer to inner space
    Loop           = 0x4,   // PAYLOAD_Bits is a FMovieSceneTimeWarpLoop
    Clamp          = 0x5,   // PAYLOAD_Bits is a FMovieSceneTimeWarpClamp

    LoopFloat      = 0x6,   // PAYLOAD_Bits is a FMovieSceneTimeWarpLoopFloat
    ClampFloat     = 0x7,   // PAYLOAD_Bits is a FMovieSceneTimeWarpClampFloat

    // Max of 8 types supported
};

[JsonConverter(typeof(FMovieSceneTimeWarpVariantConverter))]
public class FMovieSceneTimeWarpVariant : IUStruct
{
    public EMovieSceneTimeWarpType Type;
    public readonly byte[] data;
    public readonly FAssetArchive Owner;

    public FMovieSceneTimeWarpVariant(FAssetArchive Ar)
    {
        var isLiteral = Ar.ReadBoolean();
        data = Ar.ReadBytes(8);
        Type = isLiteral ? EMovieSceneTimeWarpType.FixedPlayRate : (EMovieSceneTimeWarpType) (data[7] & 0x7) + 1;
    }
}

public class FMovieSceneTimeWarpVariantConverter : JsonConverter<FMovieSceneTimeWarpVariant>
{
    public override void WriteJson(JsonWriter writer, FMovieSceneTimeWarpVariant value, JsonSerializer serializer)
    {
        var data = value.data;
        writer.WriteStartObject();
        switch (value.Type)
        {
            case EMovieSceneTimeWarpType.FixedPlayRate:
                writer.WritePropertyName("PlayRate");
                writer.WriteValue(BitConverter.ToDouble(data));
                break;
            case EMovieSceneTimeWarpType.Custom:
                // probably wrong
                writer.WritePropertyName("ReferenceToSelf");
                serializer.Serialize(writer, new FPackageIndex(value.Owner, BitConverter.ToInt32(data)));
                break;
            case EMovieSceneTimeWarpType.FixedTime:
                writer.WritePropertyName("FrameNumber");
                writer.WriteValue(BitConverter.ToInt32(data));
                break;
            case EMovieSceneTimeWarpType.FrameRate:
                writer.WritePropertyName("FrameRate");
                var numerator = data[0] | data[1] << 8 | data[2] << 16;
                var denominator = data[3] | data[4] << 8 | data[5] << 16;
                var frameRate = new FFrameRate(numerator, denominator);
                serializer.Serialize(writer, frameRate);
                break;
            case EMovieSceneTimeWarpType.Loop:
                writer.WritePropertyName("Duration");
                writer.WriteValue(BitConverter.ToInt32(data));
                break;
            case EMovieSceneTimeWarpType.Clamp:
                writer.WritePropertyName("max");
                writer.WriteValue(BitConverter.ToInt32(data));
                break;
            case EMovieSceneTimeWarpType.LoopFloat:
                writer.WritePropertyName("Duration");
                writer.WriteValue(BitConverter.ToSingle(data));
                break;
            case EMovieSceneTimeWarpType.ClampFloat:
                writer.WritePropertyName("StartTime");
                writer.WriteValue(BitConverter.ToSingle(data));
                break;

        };
        writer.WriteEndObject();
    }

    public override FMovieSceneTimeWarpVariant? ReadJson(JsonReader reader, Type objectType, FMovieSceneTimeWarpVariant? existingValue, bool hasExistingValue, JsonSerializer serializer)
    {
        throw new NotImplementedException();
    }
}
