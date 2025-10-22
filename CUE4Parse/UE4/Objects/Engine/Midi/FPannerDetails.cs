using System;
using CUE4Parse.UE4.Assets.Readers;
using Newtonsoft.Json;

namespace CUE4Parse.UE4.Objects.Engine.Midi;

public enum EPannerMode : byte
{
    LegacyStereo,
    Stere,
    Surround,
    PolarSurround,
    DirectAssignment
}

public enum ESpeakerChannelAssignment : byte
{
    LeftFront,
    RightFront,
    Center,
    LFE,
    LeftSurround,
    RightSurround,
    LeftRear,
    RightRear,
    FrontPair,
    CenterAndLFE,
    SurroundPair,
    RearPair,
    AmbisonicW,
    AmbisonicX,
    AmbisonicY,
    AmbisonicZ,
    AmbisonicWXPair,
    AmbisonicYZPair,
    UnspecifiedMono
}

[JsonConverter(typeof(FPannerDetailsConverter))]
public class FPannerDetails : IUStruct
{
    public byte Version;
    public EPannerMode Mode;
    public ESpeakerChannelAssignment ChannelAssignment;
    public float Pan;
    public float EdgeProximity;

    public FPannerDetails(FAssetArchive Ar)
    {
        Version = Ar.Read<byte>();
        Mode = Ar.Read<EPannerMode>();
        if (Mode == EPannerMode.DirectAssignment)
        {
            ChannelAssignment = Ar.Read<ESpeakerChannelAssignment>();
        }
        else
        {
            Pan = Ar.Read<float>();
            EdgeProximity = Ar.Read<float>();
        }
    }
}

public class FPannerDetailsConverter : JsonConverter<FPannerDetails>
{
    public override void WriteJson(JsonWriter writer, FPannerDetails value, JsonSerializer serializer)
    {
        writer.WriteStartObject();

        writer.WritePropertyName("Version");
        writer.WriteValue(value.Version);

        writer.WritePropertyName("Mode");
        writer.WriteValue(value.Mode.ToString());

        if (value.Mode == EPannerMode.DirectAssignment)
        {
            writer.WritePropertyName("ChannelAssignment");
            writer.WriteValue(value.ChannelAssignment.ToString());
        }
        else
        {
            writer.WritePropertyName("Pan");
            writer.WriteValue(value.Pan);
            writer.WritePropertyName("EdgeProximity");
            writer.WriteValue(value.EdgeProximity);
        }

        writer.WriteEndObject();
    }

    public override FPannerDetails ReadJson(JsonReader reader, Type objectType, FPannerDetails existingValue, bool hasExistingValue, JsonSerializer serializer)
    {
        throw new NotImplementedException();
    }
}