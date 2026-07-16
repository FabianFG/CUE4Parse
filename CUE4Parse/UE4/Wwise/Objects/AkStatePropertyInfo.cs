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

    public AkStatePropertyInfo(FWwiseArchive Ar)
    {
        PropertyId = Ar.Read7BitEncodedIntBE();
        AccumType = Ar.Read<EAkRtpcAccum>();
        if (Ar.Version > 126)
        {
            InDb = Ar.Read<byte>();
        }
    }
}
