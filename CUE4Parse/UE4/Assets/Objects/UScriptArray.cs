using System;
using System.Collections.Generic;
using CUE4Parse.GameTypes.DaysGone.Assets;
using CUE4Parse.UE4.Assets.Objects.Properties;
using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Exceptions;
using CUE4Parse.UE4.Versions;
using Newtonsoft.Json;
using Serilog;

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

    public UScriptArray(FAssetArchive Ar, FPropertyTagData? tagData, ReadType type, int size)
    {
        InnerType = tagData?.InnerType ?? throw new ParserException(Ar, "UScriptArray needs inner type");
        var elementCount = Ar.Read<int>();

        if (elementCount > Ar.Length - Ar.Position)
        {
            throw new ParserException(Ar,
                $"ArrayProperty element count {elementCount} is larger than the remaining archive size {Ar.Length - Ar.Position}");
        }

        if (Ar.HasUnversionedProperties)
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
            if (Ar.Game == EGame.GAME_DaysGone && InnerType == "StructProperty")
            {
                var count = elementCount > 0 ? elementCount : 1;
                var elemsize = (size - sizeof(int)) / count;
                InnerTagData = DaysGoneProperties.GetArrayStructType(tagData.Name, elemsize);
            }
        }

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
                    Log.Debug($"Failed to read array property of type {InnerType} at ${Ar.Position}, index {i}");
            }
            return;
        }

        for (var i = 0; i < elementCount; i++)
        {
            var property = FPropertyTagType.ReadPropertyTagType(Ar, InnerType, InnerTagData, readType);
            if (property != null)
                Properties.Add(property);
            else
                Log.Debug($"Failed to read array property of type {InnerType} at ${Ar.Position}, index {i}");
        }
    }

    public override string ToString() => $"{InnerType}[{Properties.Count}]";
}
