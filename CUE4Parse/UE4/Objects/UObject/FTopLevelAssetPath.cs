using System.Text;
using CUE4Parse.UE4.Readers;
using CUE4Parse.UE4.Writers;
using Newtonsoft.Json;

namespace CUE4Parse.UE4.Objects.UObject;

[JsonConverter(typeof(FTopLevelAssetPathConverter))]
public readonly struct FTopLevelAssetPath : IUStruct, ISerializable
{
    public readonly FName PackageName;
    public readonly FName AssetName;

    public FTopLevelAssetPath(FArchive Ar)
    {
        PackageName = Ar.ReadFName();
        AssetName = Ar.ReadFName();
    }

    public void Serialize(FArchiveWriter Ar)
    {
        Ar.Serialize(PackageName);
        Ar.Serialize(AssetName);
    }

    public FTopLevelAssetPath(FName packageName, FName assetName)
    {
        PackageName = packageName;
        AssetName = assetName;
    }

    public override string ToString()
    {
        var builder = new StringBuilder();
        if (PackageName.IsNone) return string.Empty;
        builder.Append(PackageName);
        if (!AssetName.IsNone) builder.Append('.').Append(AssetName);
        return builder.ToString();
    }
}