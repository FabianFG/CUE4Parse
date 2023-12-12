using CUE4Parse.UE4.Assets.Objects;
using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Assets.Utils;
using CUE4Parse.UE4.Objects.Core.Misc;
using CUE4Parse.UE4.Objects.UObject;
using CUE4Parse.UE4.Writers;
using Newtonsoft.Json;

namespace CUE4Parse.UE4.Assets.Exports.Material;

[StructFallback]
public class FTextureParameterValue : IUStruct, ISerializable
{
    [JsonIgnore]
    public string Name => (!ParameterName.IsNone ? ParameterName : ParameterInfo.Name).Text;
    public readonly FName ParameterName;
    public readonly FMaterialParameterInfo ParameterInfo;
    public readonly FPackageIndex ParameterValue; // UTexture
    public readonly FGuid ExpressionGUID;

    public FTextureParameterValue(FStructFallback fallback)
    {
        ParameterName = fallback.GetOrDefault<FName>(nameof(ParameterName));
        ParameterInfo = fallback.GetOrDefault<FMaterialParameterInfo>(nameof(ParameterInfo));
        ParameterValue = fallback.GetOrDefault<FPackageIndex>(nameof(ParameterValue));
        ExpressionGUID = fallback.GetOrDefault<FGuid>(nameof(ExpressionGUID));
    }

    public FTextureParameterValue(FAssetArchive Ar)
    {
        ParameterInfo = new FMaterialParameterInfo(Ar);
        ParameterValue = new FPackageIndex(Ar);
        ExpressionGUID = Ar.Read<FGuid>();
    }

    public void Serialize(FArchiveWriter Ar)
    {
        Ar.Serialize(ParameterInfo);
        Ar.Serialize(ParameterValue);
        Ar.Serialize(ExpressionGUID);
    }

    public override string ToString() => $"{Name}: {ParameterValue}";
}