using CUE4Parse.GameTypes.DaysGone.Assets;
using CUE4Parse.UE4.Assets.Objects.Properties;
using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Exceptions;
using CUE4Parse.UE4.Objects.UObject;
using CUE4Parse.UE4.Versions;
using Newtonsoft.Json;

namespace CUE4Parse.UE4.Assets.Objects;

[JsonConverter(typeof(UScriptArrayConverter))]
public class UScriptArray
{
    
    public readonly string InnerType;
    public readonly FPropertyTagData? InnerTagData;
    public readonly List<FPropertyTagType> Properties;

    public UScriptArray(string innerType)
    {
        InnerType = innerType;
        InnerTagData = null;
        Properties = [];
    }

    public UScriptArray(List<FPropertyTagType> properties, string innerType, FPropertyTagData? innerTagData = null)
    {
        InnerType = innerType;
        InnerTagData = innerTagData;
        Properties = properties;
    }

    public UScriptArray(FAssetArchive Ar, FPropertyTagData? tagData, ReadType type, int size)
    {
        InnerType = tagData?.InnerType;
        var elementCount = Ar.Read<int>();

        if (elementCount > Ar.Length - Ar.Position)
        {
            throw new ParserException(Ar,
                $"ArrayProperty element count {elementCount} is larger than the remaining archive size {Ar.Length - Ar.Position}");
        }

        if (Ar.Game < GAME_UE4_0)
        {
            if (!Ar.HasUnversionedProperties &&
                tagData?.Name is not null &&
                Ar.Owner?.Provider?.MappingsForGame?.Types is { } mappingTypes &&
                TryGetArrayInnerType(
                    mappingTypes,
                    Ar.StructTypeStack.Peek(),
                    tagData.Name,
                    out var innerType,
                    out var innerTagData))
            {
                InnerType = innerType;
                InnerTagData = innerTagData;
            }

            if (InnerType == null)
            {
                InnerType = "StructProperty";
            }
        }
        else if (Ar.HasUnversionedProperties || type is ReadType.RAW)
        {
            InnerTagData = tagData.InnerTypeData;
        }
        else if (Ar.Ver >= EUnrealEngineObjectUE5Version.PROPERTY_TAG_COMPLETE_TYPE_NAME && InnerType == "StructProperty")
        {
            InnerTagData = tagData.InnerTypeData;
        }
        else if (Ar.Ver >= EUnrealEngineObjectUE4Version.INNER_ARRAY_TAG_INFO && InnerType == "StructProperty")
        {
            InnerTagData = new FPropertyTag(Ar, false).TagData;
            if (InnerTagData == null)
                throw new ParserException(Ar, $"Couldn't read ArrayProperty with inner type {InnerType}");
        }
        else
        {
            if (Ar.Game == GAME_DaysGone && InnerType == "StructProperty")
            {
                var count = elementCount > 0 ? elementCount : 1;
                var elemsize = (size - sizeof(int)) / count;
                InnerTagData = DaysGoneProperties.GetArrayStructType(tagData.Name, elemsize);
            }
        }

        if (InnerType == null) throw new ParserException(Ar, "UScriptArray needs inner type");

        Properties = new List<FPropertyTagType>(elementCount);
        if (elementCount == 0) return;

        var readType = type == ReadType.RAW ? ReadType.RAW : ReadType.ARRAY;
        // special case for ByteProperty, as it can be read as a single byte or as EnumProperty
        if (InnerType == "ByteProperty")
        {
            var enumprop = (InnerTagData?.EnumName != null && !InnerTagData.EnumName.Equals("None", StringComparison.OrdinalIgnoreCase)) || Ar.TestReadFName();
            if (!Ar.HasUnversionedProperties) enumprop = (size - sizeof(int)) / elementCount > 1;
            for (var i = 0; i < elementCount; i++)
            {
                var property = enumprop ? (FPropertyTagType?) new EnumProperty(Ar, InnerTagData, readType) : new ByteProperty(Ar, readType);
                if (property != null)
                    Properties.Add(property);
                else
                    Log.Debug("Failed to read array property of type {InnerType} at {Position}, index {Index}", InnerType, Ar.Position, i);
            }
            return;
        }

        for (var i = 0; i < elementCount; i++)
        {
            var property = FPropertyTagType.ReadPropertyTagType(Ar, InnerType, InnerTagData, readType);
            if (property != null)
                Properties.Add(property);
            else
                Log.Debug("Failed to read array property of type {InnerType} at {Position}, index {Index}", InnerType, Ar.Position, i);
        }
    }

    private static bool TryGetArrayInnerType(
        IDictionary<string, MappingsProvider.Struct> mappingTypes,
        string structName,
        FName propertyName,
        out string? innerType,
        out FPropertyTagData? innerTagData)
    {
        innerType = null;
        innerTagData = null;

        var currentStructName = structName;
        while (mappingTypes.TryGetValue(currentStructName, out var mappingStruct))
        {
            var property = mappingStruct.Properties.Values.FirstOrDefault(p => p.Name == propertyName);

            if (property?.MappingType is { Type: "ArrayProperty" } mapType)
            {
                var inner = mapType.InnerType;
                if (inner?.Type == "StructProperty")
                {
                    innerType = "StructProperty";
                    innerTagData = new FPropertyTagData { Type = "StructProperty", StructType = inner.StructType, Name = inner.StructType };
                }
                else
                {
                    innerType = inner?.Type;
                }

                return true;
            }

            if (mappingStruct.SuperType is null)
                return false;

            currentStructName = mappingStruct.SuperType;
        }

        return false;
    }

    public override string ToString() => $"{InnerType}[{Properties.Count}]";
}
