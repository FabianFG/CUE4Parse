using CUE4Parse.UE4.Readers;
using CUE4Parse.UE4.Wwise.Enums;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace CUE4Parse.UE4.Wwise.Objects;

public readonly struct AkAuxParams
{
    [JsonConverter(typeof(StringEnumConverter))]
    public readonly EAuxParams AuxParams;
    public readonly uint[] AuxIds = [];
    public readonly uint ReflectionsAuxBus;

    public AkAuxParams(FArchive Ar)
    {
        AuxParams = Ar.Read<EAuxParams>();
        if (AuxParams.HasFlag(EAuxParams.HasAux))
        {
            AuxIds = Ar.ReadArray(4, Ar.Read<uint>);
        }

        if (WwiseVersions.Version > 134)
        {
            ReflectionsAuxBus = Ar.Read<uint>();
        }
    }
}
