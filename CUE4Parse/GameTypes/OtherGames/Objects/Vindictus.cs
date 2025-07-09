using CUE4Parse.UE4;
using CUE4Parse.UE4.Assets.Objects;
using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Objects.UObject;
using Serilog;

namespace CUE4Parse.GameTypes.OtherGames.Objects;

public struct FAnyValue : IUStruct
{
    public readonly FStructFallback? NonConstStruct;

    public FAnyValue(FAssetArchive Ar)
    {
        _ = Ar.Read<byte>(); // Old Version

        var strucindex = new FPackageIndex(Ar);
        if (strucindex.IsNull)
            return;

        if (strucindex.TryLoad<UStruct>(out var struc))
        {
            NonConstStruct = new FStructFallback(Ar, struc);
        }
        else if (strucindex.ResolvedObject is { } obj)
        {
            NonConstStruct = new FStructFallback(Ar, obj.Name.ToString());
        }
        else
        {
            Log.Warning("Failed to read FAnyValue of type {0}, skipping it", strucindex.ResolvedObject?.GetFullName());
        }
    }
}
