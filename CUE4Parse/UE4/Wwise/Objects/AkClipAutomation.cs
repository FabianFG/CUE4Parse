using CUE4Parse.UE4.Readers;
using CUE4Parse.UE4.Wwise.Enums;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace CUE4Parse.UE4.Wwise.Objects;

public readonly struct AkClipAutomation
{
    public readonly uint ClipIndex;
    [JsonConverter(typeof(StringEnumConverter))]
    public readonly EAkClipAutomationType AutoType;
    public readonly AkRtpcGraphPoint[] GraphPoints;

    public AkClipAutomation(FArchive Ar)
    {
        ClipIndex = Ar.Read<uint>();
        AutoType = Ar.Read<EAkClipAutomationType>();
        GraphPoints = Ar.ReadArray((int) Ar.Read<uint>(), () => new AkRtpcGraphPoint(Ar));
    }
}
