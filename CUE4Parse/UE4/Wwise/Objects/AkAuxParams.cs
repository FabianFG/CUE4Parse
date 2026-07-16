using CUE4Parse.UE4.Wwise.Enums.Flags;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace CUE4Parse.UE4.Wwise.Objects;

public readonly struct AkAuxParams
{
    public readonly bool OverrideGameAuxSends;
    public readonly bool UseGameAuxSends;
    public readonly bool OverrideUserAuxSends;

    [JsonConverter(typeof(StringEnumConverter))]
    public readonly EAuxParamsFlags AuxParams;

    public readonly uint[] AuxIds;
    public readonly uint ReflectionsAuxBus;

    public AkAuxParams(FWwiseArchive Ar)
    {
        AuxIds = [];

        bool hasAux;
        if (Ar.Version <= 89)
        {
            OverrideGameAuxSends = Ar.ReadBool();
            UseGameAuxSends = Ar.ReadBool();
            OverrideUserAuxSends = Ar.ReadBool();
            hasAux = Ar.ReadBool();

            if (OverrideUserAuxSends)
                AuxParams |= EAuxParamsFlags.OverrideUserAuxSends;
            if (hasAux)
                AuxParams |= EAuxParamsFlags.HasAux;
        }
        else
        {
            AuxParams = Ar.Read<EAuxParamsFlags>();
            hasAux = AuxParams.HasFlag(EAuxParamsFlags.HasAux);
        }

        if (hasAux)
        {
            AuxIds = Ar.ReadArray(4, Ar.Read<uint>);
        }

        if (Ar.Version > 134)
        {
            ReflectionsAuxBus = Ar.Read<uint>();
        }
    }
}
