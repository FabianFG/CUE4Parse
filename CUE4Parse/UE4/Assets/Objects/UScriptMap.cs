using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Exceptions;
using System.Collections.Generic;
using CUE4Parse.UE4.Assets.Objects.Properties;
using Newtonsoft.Json;
using CUE4Parse.UE4.Versions;
using CUE4Parse.GameTypes.DaysGone.Assets;
using CUE4Parse.GameTypes.SOD2.Assets;

namespace CUE4Parse.UE4.Assets.Objects;

[JsonConverter(typeof(UScriptMapConverter))]
public class UScriptMap
{
    public Dictionary<FPropertyTagType, FPropertyTagType?> Properties;

    public UScriptMap()
    {
        Properties = [];
    }

    public UScriptMap(FAssetArchive Ar, FPropertyTagData tagData, ReadType readType)
    {
        if (Ar.Ver < EUnrealEngineObjectUE4Version.PROPERTY_TAG_SET_MAP_SUPPORT)
        {
            (tagData.InnerType, tagData.ValueType) = Ar.Game switch
            {
                EGame.GAME_DaysGone => DaysGoneProperties.GetMapPropertyTypes(tagData.Name),
                EGame.GAME_StateOfDecay2 => SOD2Properties.GetMapPropertyTypes(tagData.Name),
                EGame.GAME_WeHappyFew => tagData.Name switch
                {
                    "PointMap" or "JunctionMap" or "RoadMap" => ("IntProperty", "StructProperty"),
                    "States" => ("NameProperty", "StructProperty"),
                    _ => (null, null)
                },
                _ => (null, null)
            };
        }

        if (Ar.Game == EGame.GAME_UE4_27 && tagData.Name == "LinecodeTable") // Psychonauts 2
        {
            tagData.InnerType = "LinecodeProperty";
            tagData.ValueType = "UInt32Property";
        }

        if (tagData.InnerType == null || tagData.ValueType == null)
            throw new ParserException(Ar, "Can't serialize UScriptMap without key or value type");

        if (!Ar.HasUnversionedProperties && tagData.Name is not null && Ar.Versions.MapStructTypes.TryGetValue(tagData.Name, out var mapStructTypes))
        {
            if (!string.IsNullOrEmpty(mapStructTypes.Key)) tagData.InnerTypeData = new FPropertyTagData(mapStructTypes.Key);
            if (!string.IsNullOrEmpty(mapStructTypes.Value)) tagData.ValueTypeData = new FPropertyTagData(mapStructTypes.Value);
        }

        if (readType != ReadType.RAW)
        {
            var numKeysToRemove = Ar.Read<int>();
            for (var i = 0; i < numKeysToRemove; i++)
            {
                FPropertyTagType.ReadPropertyTagType(Ar, tagData.InnerType, tagData.InnerTypeData, ReadType.MAP);
            }
        }

        var type = readType == ReadType.RAW ? ReadType.RAW : ReadType.MAP;
        var numEntries = Ar.Read<int>();
        Properties = new Dictionary<FPropertyTagType, FPropertyTagType?>(numEntries);
        for (var i = 0; i < numEntries; i++)
        {
            var isReadingValue = false;
            try
            {
                var key = FPropertyTagType.ReadPropertyTagType(Ar, tagData.InnerType, tagData.InnerTypeData, type);
                isReadingValue = true;
                var value = FPropertyTagType.ReadPropertyTagType(Ar, tagData.ValueType, tagData.ValueTypeData, type);
                Properties[key ?? new StrProperty($"UNK_Entry_{i}")] = value;
            }
            catch (ParserException e)
            {
                 throw new ParserException(Ar, $"Failed to read {(isReadingValue ? "value" : "key")} for index {i} in map", e);
            }
        }
    }
}
