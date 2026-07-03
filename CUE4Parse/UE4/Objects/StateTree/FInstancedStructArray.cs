using CUE4Parse.UE4.Assets.Objects;
using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Objects.UObject;

namespace CUE4Parse.UE4.Objects.StateTree;

public class FInstancedStructArray : IUStruct
{
    public FScriptStruct?[] Items = [];

    public FInstancedStructArray() { }

    public FInstancedStructArray(FAssetArchive Ar)
    {
        var version = Ar.Read<byte>();
        var nonConstStructs = Ar.ReadArray(() => new FPackageIndex(Ar));
        var numItems = nonConstStructs.Length;
        if (numItems == 0) return;

        Items = new FScriptStruct[numItems];
        for (var i = 0; i < numItems; i++)
        {
            Items[i] = FScriptStruct.ReadInstancedStruct(Ar, nonConstStructs[i]);
        }
    }
}
