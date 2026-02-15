using CUE4Parse.UE4.Readers;
using CUE4Parse.UE4.Wwise.Enums;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace CUE4Parse.UE4.Wwise.Objects;

public readonly struct AkStatePropertyInfo
{
    public readonly int PropertyId;
    [JsonConverter(typeof(StringEnumConverter))]
    public readonly EAkRtpcAccum AccumType;
    public readonly byte InDb;

    public AkStatePropertyInfo(FArchive Ar)
    {
        PropertyId = WwiseReader.Read7BitEncodedIntBE(Ar);
        AccumType = Ar.Read<EAkRtpcAccum>();
        if (WwiseVersions.Version > 126)
        {
            InDb = Ar.Read<byte>();
        }
    }
}
