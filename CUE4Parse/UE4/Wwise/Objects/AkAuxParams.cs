using System.Collections.Generic;
using CUE4Parse.UE4.Readers;
using CUE4Parse.UE4.Wwise.Enums;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace CUE4Parse.UE4.Wwise.Objects;

public class AkAuxParams
{
    [JsonConverter(typeof(StringEnumConverter))]
    public readonly EAuxParams AuxParams;
    public readonly List<uint> AuxIds;
    public readonly uint ReflectionsAuxBus;

    public AkAuxParams(FArchive ar)
    {
        AuxIds = [];
        AuxParams = ar.Read<EAuxParams>();
        if (AuxParams.HasFlag(EAuxParams.HasAux))
        {
            for (int i = 0; i < 4; i++)
            {
                AuxIds.Add(ar.Read<uint>());
            }
        }

        if (WwiseVersions.Version > 135)
        {
            ReflectionsAuxBus = ar.Read<uint>();
        }
    }
}
