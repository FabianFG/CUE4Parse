using CUE4Parse.UE4.Assets.Readers;
using Newtonsoft.Json;

namespace CUE4Parse.UE4.Assets.Objects.Properties;

[JsonConverter(typeof(AssetObjectPropertyConverter))]
public class AssetObjectProperty : FPropertyTagType<string>
{
    public readonly IPackage? Owner;

    public AssetObjectProperty(string value, IPackage? owner)
    {
        Owner = owner;
        Value = value;
    }

    public AssetObjectProperty(FAssetArchive Ar, ReadType type)
    {
        Owner = Ar.Owner;
        Value = type switch
        {
            ReadType.ZERO => string.Empty,
            _ => Ar.ReadFString()
        };
    }
}
