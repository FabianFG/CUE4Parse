using System.Collections.Generic;
using CUE4Parse.UE4.Assets.Objects;
using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Exceptions;

namespace CUE4Parse.UE4.Objects.UObject;

public struct FUniversalObjectLocatorFragment: IUStruct
{
    public FStructFallback? FragmentStruct;

    public FUniversalObjectLocatorFragment(FAssetArchive Ar)
    {
        var FragmentTypeID = Ar.ReadFName();
        if (FragmentTypeID.IsNone) return;

        if (FragmentTypeRegistry.TryGetValue(FragmentTypeID.Text, out var structType))
        {
            FragmentStruct = new FStructFallback(Ar, structType);
        }
        else
        {
            throw new ParserException($"Unknown FragmentTypeID : {FragmentTypeID}");
        }
    }

    private static Dictionary<string, string> FragmentTypeRegistry = new()
    {
        { "actor", "DirectPathObjectLocator" },
        { "animinst", "DirectPathObjectLocator" },
        { "subobj", "SubObjectLocator" },
    };
}
