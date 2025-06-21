using System.Collections.Generic;
using CUE4Parse.UE4.Assets.Readers;
using Newtonsoft.Json;

namespace CUE4Parse.UE4.Assets.Exports.AudioSynesthesia;

public class UConstantQNRT : UObject
{
    public float DurationInSeconds;
    public bool bIsSortedChronologically;
    public Dictionary<int, FConstantQFrame[]> ChannelCQTFrames;
    public Dictionary<int, FFloatInterval> ChannelCQTIntervals;

    public override void Deserialize(FAssetArchive Ar, long validPos)
    {
        base.Deserialize(Ar, validPos);
        DurationInSeconds = Ar.Read<float>();
        bIsSortedChronologically = Ar.ReadBoolean();
        ChannelCQTFrames = Ar.ReadMap(Ar.Read<int>, () => Ar.ReadArray(() => new FConstantQFrame(Ar)));
        ChannelCQTIntervals = Ar.ReadMap(Ar.Read<int>, Ar.Read<FFloatInterval>);
    }

    protected internal override void WriteJson(JsonWriter writer, JsonSerializer serializer)
    {
        base.WriteJson(writer, serializer);
        writer.WritePropertyName("DurationInSeconds");
        writer.WriteValue(DurationInSeconds);
        writer.WritePropertyName("bIsSortedChronologically");
        writer.WriteValue(bIsSortedChronologically);
        writer.WritePropertyName("ChannelLoudnessArrays");
        serializer.Serialize(writer, ChannelCQTFrames);
        writer.WritePropertyName("ChannelLoudnessIntervals");
        serializer.Serialize(writer, ChannelCQTIntervals);
    }
}

public class FConstantQFrame
{
    public int Channel;
    public float Timestamp;
    public float[] Spectrum;

    public FConstantQFrame(FAssetArchive Ar)
    {
        Channel = Ar.Read<int>();
        Timestamp = Ar.Read<float>();
        Spectrum = Ar.ReadArray<float>();
    }
}
