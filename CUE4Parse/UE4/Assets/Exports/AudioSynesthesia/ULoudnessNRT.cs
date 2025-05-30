using System.Collections.Generic;
using System.Runtime.InteropServices;
using CUE4Parse.UE4.Assets.Readers;
using Newtonsoft.Json;

namespace CUE4Parse.UE4.Assets.Exports.AudioSynesthesia;

public class ULoudnessNRT : UObject
{
    public float DurationInSeconds;
    public bool bIsSortedChronologically;
    public Dictionary<int, FLoudnessDatum[]> ChannelLoudnessArrays;
    public Dictionary<int, FFloatInterval> ChannelLoudnessIntervals;

    public override void Deserialize(FAssetArchive Ar, long validPos)
    {
        base.Deserialize(Ar, validPos);
        DurationInSeconds = Ar.Read<float>();
        bIsSortedChronologically = Ar.ReadBoolean();
        ChannelLoudnessArrays = Ar.ReadMap(Ar.Read<int>, Ar.ReadArray<FLoudnessDatum>);
        ChannelLoudnessIntervals = Ar.ReadMap(Ar.Read<int>, Ar.Read<FFloatInterval>);
    }

    protected internal override void WriteJson(JsonWriter writer, JsonSerializer serializer)
    {
        base.WriteJson(writer, serializer);
        writer.WritePropertyName("DurationInSeconds");
        writer.WriteValue(DurationInSeconds);
        writer.WritePropertyName("bIsSortedChronologically");
        writer.WriteValue(bIsSortedChronologically);
        writer.WritePropertyName("ChannelLoudnessArrays");
        serializer.Serialize(writer, ChannelLoudnessArrays);
        writer.WritePropertyName("ChannelLoudnessIntervals");
        serializer.Serialize(writer, ChannelLoudnessIntervals);
    }
}

[StructLayout(LayoutKind.Sequential, Pack = 4)]
public struct FLoudnessDatum
{
    public int Channel;
    public float Timestamp;
    public float Energy;
    public float Loudness;
}


[StructLayout(LayoutKind.Sequential, Pack = 4)]
public struct FFloatInterval
{
    public float min;
    public float max;
}
