using CUE4Parse.MappingsProvider;
using CUE4Parse.UE4.Assets.Exports;
using CUE4Parse.UE4.Assets.Objects.Properties;
using CUE4Parse.UE4.Assets.Objects.Unversioned;
using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Objects.UObject;
using Newtonsoft.Json;

namespace CUE4Parse.UE4.Assets.Objects;

[JsonConverter(typeof(FStructFallbackConverter))]
[SkipObjectRegistration]
public class FStructFallback : AbstractPropertyHolder, IUStruct
{

    public FStructFallback() => Properties = [];

    public FStructFallback(List<FPropertyTag> properties) => Properties = properties;

    public FStructFallback(FAssetArchive Ar, string? structType) : this(Ar, ResolveStructType(Ar, structType)) { }

    private FStructFallback(FAssetArchive Ar, (UScriptClass? Struct, string? Identifier) resolvedType) :
        this(Ar, resolvedType.Struct, resolvedType.Identifier) { }

    public FStructFallback(FAssetArchive Ar, UStruct? structType = null) : this(Ar, structType, null) { }

    public FStructFallback(FAssetArchive Ar, UStruct? structType, string? fullTypeIdentifier)
    {
        fullTypeIdentifier ??= structType is UScriptClass { FullTypeIdentifier: { } scriptIdentifier }
            ? scriptIdentifier
            : structType?.Outer != null ? structType.GetPathName() : null;
        if (Ar.HasUnversionedProperties)
        {
            if (structType == null) throw new ArgumentException("For unversioned struct fallback the struct type cannot be null", nameof(structType));
            UObject.DeserializePropertiesUnversioned(Properties = [], Ar, structType, fullTypeIdentifier);
        }
        else
        {
            UObject.DeserializePropertiesTagged(Properties = [], Ar, true, structType, fullTypeIdentifier);
        }
    }

    public FStructFallback(FAssetArchive Ar, string? structType, FRawHeader rawHeader, ReadType type = ReadType.NORMAL)
    {
        ArgumentException.ThrowIfNullOrEmpty(structType, nameof(structType));
        structType = Ar.ResolveTypeIdentifier(structType);
        UObject.DeserializeRawProperties(Properties = [], Ar,
            new UScriptClass(TypeMappings.GetShortTypeName(structType), structType), rawHeader, type,
            structType);
    }

    private static (UScriptClass? Struct, string? Identifier) ResolveStructType(FAssetArchive Ar,
        string? structType)
    {
        if (structType is null)
            return (null, null);

        var identifier = Ar.ResolveTypeIdentifier(structType);
        return (new UScriptClass(TypeMappings.GetShortTypeName(identifier), identifier), identifier);
    }

    [Obsolete("Deprecated, please use FScriptStruct.ReadInstancedStructWithoutSerialSize", true)]
    public static FStructFallback? ReadInstancedStruct(FAssetArchive Ar)
    {
        var structType = new FPackageIndex(Ar);
        return ReadInstancedStruct(Ar, structType);
    }

    [Obsolete("Deprecated, please use FScriptStruct.ReadInstancedStructWithoutSerialSize", true)]
    public static FStructFallback? ReadInstancedStruct(FAssetArchive Ar, FPackageIndex structType)
    {
        if (structType is null || structType.IsNull)
            return null;

        FStructFallback? result = null;
        var fullTypeIdentifier = structType.ResolvedObject?.GetPathName();
        if (structType.TryLoad<UStruct>(out var struc))
        {
            result = new FStructFallback(Ar, struc, fullTypeIdentifier);
        }
        else if (structType.ResolvedObject is { } obj)
        {
            result = new FStructFallback(Ar, obj.GetPathName());
        }
        else
        {
            Log.Warning("Failed to read Struct of type {0}, skipping it", structType.ResolvedObject?.GetFullName());
        }
        return result;
    }
}
