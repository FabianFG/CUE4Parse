using System;
using System.Collections.Generic;
using System.Linq;
using CUE4Parse.MappingsProvider;
using CUE4Parse.UE4.Assets.Objects;
using CUE4Parse.UE4.Assets.Objects.Properties;
using CUE4Parse.UE4.Exceptions;
using CUE4Parse.UE4.Objects.UObject;
using CUE4Parse.UE4.Readers;

namespace CUE4Parse.GameTypes.Aion2.Objects;

public static class FAion2PropertyReader
{
    public static FPropertyTagType? ReadPropertyTagType(FArchive Ar, TypeMappings mappings, string? propertyType, FPropertyTagData? tagData, bool readtag = true,  ReadType type = ReadType.NORMAL)
    {
        return propertyType switch
        {
            "ArrayProperty" => new ArrayProperty(ReadArray(Ar, mappings, tagData, readtag)),
            "BoolProperty" => new BoolProperty(Ar.ReadFlag()),
            "ByteProperty" => (tagData?.EnumName != null && !tagData.EnumName.Equals("None", StringComparison.OrdinalIgnoreCase))
                ? (FPropertyTagType?) new EnumProperty(Ar.ReadFName())
                : new ByteProperty(Ar.Read<byte>()),
            "DoubleProperty" => new DoubleProperty(Ar.Read<double>()),
            "EnumProperty" => new EnumProperty(Ar.ReadFName()),
            "FloatProperty" => new FloatProperty(Ar.Read<float>()),
            "Int16Property" => new Int16Property(Ar.Read<short>()),
            "Int64Property" => new Int64Property(Ar.Read<long>()),
            "Int8Property" => new Int8Property(Ar.Read<sbyte>()),
            "IntProperty" => new IntProperty(Ar.Read<int>()),
            "NameProperty" => new NameProperty(Ar.ReadFName()),
            "SetProperty" => new SetProperty(ReadSet(Ar, mappings, tagData)),
            "MapProperty" => new MapProperty(ReadMap(Ar, mappings, tagData)),
            "StrProperty" => new StrProperty(Ar.ReadFString()),
            "StructProperty" => new StructProperty(ReadStruct(Ar, mappings, tagData?.StructType)),
            "UInt16Property" => new UInt16Property(Ar.Read<ushort>()),
            "UInt32Property" => new UInt32Property(Ar.Read<uint>()),
            "UInt64Property" => new UInt64Property(Ar.Read<ulong>()),
            _ => null,
        };

        void SkipStructTag(FArchive Ar)
        {
            Ar.SkipFString();
            Ar.SkipFString();
            Ar.Position += 8;
            Ar.SkipFString();
            Ar.Position += 16;
            if (Ar.ReadFlag()) Ar.Position += 16;
        }

        UScriptSet ReadSet(FArchive Ar, TypeMappings mappings, FPropertyTagData? tagData)
        {
            var pos = Ar.Position;
            var length = Ar.Read<int>();
            for (int i = 0; i < length; i++)
            {
                ReadPropertyTagType(Ar, mappings, tagData?.InnerType, tagData?.InnerTypeData, true, ReadType.ARRAY);
            }

            return new UScriptSet(ReadArray(Ar, mappings, tagData, true).Properties);
        }

        UScriptArray ReadArray(FArchive Ar, TypeMappings mappings, FPropertyTagData? tagData, bool readtag = true)
        {
            var pos = Ar.Position;
            var length = Ar.Read<int>();
            var properties = new List<FPropertyTagType>(length);
            if (readtag && tagData?.InnerType is "StructProperty") SkipStructTag(Ar);
            for (int i = 0; i < length; i++)
            {
                properties.Add(ReadPropertyTagType(Ar, mappings, tagData?.InnerType, tagData?.InnerTypeData, readtag, ReadType.ARRAY));
            }

            return new UScriptArray(properties, tagData?.InnerType, tagData?.InnerTypeData);
        }

        UScriptMap ReadMap(FArchive Ar, TypeMappings mappings, FPropertyTagData? tagData)
        {
            var pos = Ar.Position;
            var length = Ar.Read<int>();
            for (int i = 0; i < length; i++)
            {
                ReadPropertyTagType(Ar, mappings, tagData?.InnerType, tagData?.InnerTypeData, true, ReadType.MAP);
            }
            length = Ar.Read<int>();
            var properties = new Dictionary<FPropertyTagType, FPropertyTagType?>(length);
            for (int i = 0; i < length; i++)
            {
                var key = ReadPropertyTagType(Ar, mappings, tagData?.InnerType, tagData?.InnerTypeData, false, ReadType.MAP);
                var value = ReadPropertyTagType(Ar, mappings, tagData?.ValueType, tagData?.ValueTypeData, false, ReadType.MAP);
                properties.Add(key!, value!);
            }
            return new UScriptMap(properties);
        }

        FScriptStruct ReadStruct(FArchive Ar, TypeMappings mappings, string? structName)
        {
            if (!mappings.Types.TryGetValue(structName, out var propMappings))
            {
                throw new ParserException(Ar, $"No property mappings found for struct {structName}");
            }

            var propCount = propMappings.CountProperties(true);
            var properties = new List<FPropertyTag>(propCount);
            
            foreach (var index in Enumerable.Range(0, propCount))
            {
                propMappings.TryGetValue(index, out var info);

                var tag = new FPropertyTag()
                {
                    Name = new FName(info.Name),
                    PropertyType = new FName(info.MappingType.Type),
                    ArrayIndex = info.Index,
                    ArraySize = info.ArraySize,
                    TagData = new FPropertyTagData(info.MappingType),
                };

                var pos = Ar.Position;
                try
                {
                    tag.Tag = ReadPropertyTagType(Ar, mappings, tag.PropertyType.Text, tag.TagData, true);
                }
                catch (ParserException e)
                {
                    throw new ParserException($"Failed to read FPropertyTagType {tag.TagData?.ToString() ?? tag.PropertyType.Text} {tag.Name.Text}", e);
                }

                tag.Size = (int) (Ar.Position - pos);

                if (tag.Tag != null)
                    properties.Add(tag);
                else
                    throw new ParserException(Ar, $"Failed to serialize property {info.MappingType.Type} {info.Name}. Can't proceed with serialization (Serialized {properties.Count} properties until now)");

            }

            if (structName is "ItemTableItem" && properties.FirstOrDefault(x => x.Name.Text == "ItemType")?.Tag is { } itemType
                && itemType.GetValue(typeof(EItemType)) is EItemType val)
            {
                var additionalProperties = val switch
                {
                    EItemType.Currency => ReadStruct(Ar, mappings, "ItemTableCurrencyInfo"),
                    EItemType.Equip => ReadStruct(Ar, mappings, "ItemTableEquipmentInfo"),
                    EItemType.Usable => ReadStruct(Ar, mappings, "ItemTableUsableInfo"),
                    EItemType.Misc => ReadStruct(Ar, mappings, "ItemTableMiscInfo"),
                    _ => new FScriptStruct(new FStructFallback()),
                };
                
                properties.AddRange((additionalProperties.StructType as FStructFallback)?.Properties ?? []);
            }

            return new FScriptStruct(new FStructFallback(properties));
        }
    }

    public enum EItemType
    {
        None = 0,
        Currency = 1,
        Equip = 2,
        Usable = 3,
        Misc = 4,
        Quest = 5,
        Max = 6
    }
}
