using CUE4Parse.UE4.Assets.Objects;
using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Objects.UObject;
using Serilog;

namespace CUE4Parse.UE4.Objects.StateTree;

public class FInstancedStructArray : IUStruct
{
    public FStructFallback?[] Items = [];

    public FInstancedStructArray() { }

    public FInstancedStructArray(FAssetArchive Ar)
    {
        var version = Ar.Read<byte>();

        var nonConstStructs = Ar.ReadArray(() => new FPackageIndex(Ar));
        var numItems = nonConstStructs.Length;
        if (numItems == 0) return;

        Items = new FStructFallback[numItems];
        for (var i = 0; i < numItems; i++)
        {
            Items[i] = ReadInstancedStruct(Ar, nonConstStructs[i]);
        }

        FStructFallback? ReadInstancedStruct(FAssetArchive Ar, FPackageIndex structType)
        {
            var size = Ar.Read<int>();
            var saved = Ar.Position;
            if (structType.IsNull)
            {
                Ar.Position = saved + size;
                return null;
            }

            FStructFallback? result = null;
            try
            {
                if (structType.TryLoad<UStruct>(out var struc))
                {
                    result = new FStructFallback(Ar, struc);
                }
                else if (structType.ResolvedObject is { } obj)
                {
                    result = new FStructFallback(Ar, obj.Name.ToString());
                }
                else
                {
                    Log.Warning("Failed to read Struct of type {0}, skipping it", structType.ResolvedObject?.GetFullName());
                }
            }
            catch  (Exception e)
            {
                Log.Error(e, "Failed to read instanced struct of type {0}", structType.ResolvedObject?.GetFullName());
            }
            finally
            {
                Ar.Position = saved + size;
            }

            return result;
        }
    }
}
