using CUE4Parse.UE4.Readers;
using CUE4Parse.UE4.Wwise.Enums;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace CUE4Parse.UE4.Wwise.Objects;

public readonly struct CAkConversionTable
{
    [JsonConverter(typeof(StringEnumConverter))]
    public readonly EAkCurveScaling Scaling;
    public readonly int Size; // uint for legacy versions, ushort for modern versions
    public readonly AkRtpcGraphPoint[] GraphPoints;

    public CAkConversionTable(FArchive Ar, bool readScaling = true)
    {
        if (WwiseVersions.Version <= 36)
        {
            Scaling = readScaling ? (EAkCurveScaling) Ar.Read<uint>() : EAkCurveScaling.None;
            Size = (int) Ar.Read<uint>();
        }
        else
        {
            Scaling = readScaling ? (EAkCurveScaling) Ar.Read<byte>() : EAkCurveScaling.None;
            Size = Ar.Read<ushort>();
        }

        GraphPoints = Ar.ReadArray(Size, () => new AkRtpcGraphPoint(Ar));
    }
}
