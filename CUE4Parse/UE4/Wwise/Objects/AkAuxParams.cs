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
        OverrideGameAuxSends = false;
        UseGameAuxSends = false;
        OverrideUserAuxSends = false;
        AuxParams = EAuxParamsFlags.None;
        AuxIds = [];
        ReflectionsAuxBus = 0;

        bool hasAux;
        if (Ar.Version <= 89)
        {
            OverrideGameAuxSends = Ar.Read<byte>() != 0;
            UseGameAuxSends = Ar.Read<byte>() != 0;
            OverrideUserAuxSends = Ar.Read<byte>() != 0;
            hasAux = Ar.Read<byte>() != 0;

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
