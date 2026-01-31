using CUE4Parse.UE4.Readers;
using CUE4Parse.UE4.Wwise.Enums;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace CUE4Parse.UE4.Wwise.Objects;

public readonly struct AkConversionTable
{
    [JsonConverter(typeof(StringEnumConverter))]
    public readonly EAkCurveScaling Scaling;
    public readonly dynamic Size; // uint for legacy versions, ushort for modern versions
    public readonly AkRtpcGraphPoint[] GraphPoints;

    public AkConversionTable(FArchive Ar)
    {
        if (WwiseVersions.Version <= 36)
        {
            Scaling = (EAkCurveScaling) Ar.Read<uint>();
            Size = Ar.Read<uint>();
        }
        else
        {
            Scaling = (EAkCurveScaling) Ar.Read<byte>();
            Size = Ar.Read<ushort>();
        }

        GraphPoints = Ar.ReadArray((int) Size, () => new AkRtpcGraphPoint(Ar));
    }
}
