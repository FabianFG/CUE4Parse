using System.Collections.Generic;
using CUE4Parse.UE4.Readers;
using CUE4Parse.UE4.Wwise.Enums;

namespace CUE4Parse.UE4.Wwise.Objects;

public class AkAuxParams
{
    public EAuxParams AuxParams { get; }
    public List<uint> AuxIds { get; }
    public uint ReflectionsAuxBus { get; }

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

        if (WwiseVersions.WwiseVersion > 135)
        {
            ReflectionsAuxBus = ar.Read<uint>();
        }
    }
}
