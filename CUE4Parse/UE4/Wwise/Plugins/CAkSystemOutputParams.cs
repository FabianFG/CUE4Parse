using CUE4Parse.UE4.Readers;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace CUE4Parse.UE4.Wwise.Plugins;

internal class CAkSystemOutputParams(FArchive Ar) : IAkPluginParam
{
    public AkAudioObjectDestination Destination = Ar.Read<AkAudioObjectDestination>();
}

[JsonConverter(typeof(StringEnumConverter))]
public enum AkAudioObjectDestination : int
{
    Default = 0x0,
    MainMix = 0x1,
    Passthrough = 0x2,
    SystemAudioObject = 0x3
};
