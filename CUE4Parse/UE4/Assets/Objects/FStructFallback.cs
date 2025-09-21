using System;
using CUE4Parse.UE4.Assets.Exports;
using CUE4Parse.UE4.Assets.Objects.Properties;
using CUE4Parse.UE4.Assets.Objects.Unversioned;
using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Objects.UObject;
using Newtonsoft.Json;
using Serilog;

namespace CUE4Parse.UE4.Assets.Objects;

[JsonConverter(typeof(FStructFallbackConverter))]
[SkipObjectRegistration]
public class FStructFallback : AbstractPropertyHolder, IUStruct
{
    public FStructFallback()
    {
        Properties = [];
    }

    public FStructFallback(FAssetArchive Ar, string? structType) : this(Ar, structType != null ? new UScriptClass(structType) : null) { }

    public FStructFallback(FAssetArchive Ar, UStruct? structType = null)
    {
        if (Ar.HasUnversionedProperties)
        {
            if (structType == null) throw new ArgumentException("For unversioned struct fallback the struct type cannot be null", nameof(structType));
            UObject.DeserializePropertiesUnversioned(Properties = [], Ar, structType);
        }
        else
        {
            UObject.DeserializePropertiesTagged(Properties = [], Ar, true);
        }
    }

    public FStructFallback(FAssetArchive Ar, string? structType, FRawHeader rawHeader, ReadType type = ReadType.NORMAL)
    {
        ArgumentException.ThrowIfNullOrEmpty(structType, nameof(structType));
        UObject.DeserializeRawProperties(Properties = [], Ar, new UScriptClass(structType), rawHeader, type);
    }

    public static FStructFallback? ReadInstancedStruct(FAssetArchive Ar)
    {
        var structType = new FPackageIndex(Ar);
        if (structType.IsNull) return null;

        FStructFallback? result = null;
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
        return result;
    }
}
