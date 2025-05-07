using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Exceptions;
using System.Collections.Generic;
using CUE4Parse.UE4.Assets.Objects.Properties;
using Newtonsoft.Json;
using CUE4Parse.UE4.Versions;
using CUE4Parse.GameTypes.DaysGone.Assets;

namespace CUE4Parse.UE4.Assets.Objects;

[JsonConverter(typeof(UScriptMapConverter))]
public class UScriptMap
{
    public Dictionary<FPropertyTagType, FPropertyTagType?> Properties;

    public UScriptMap()
    {
        Properties = [];
    }

    public UScriptMap(FAssetArchive Ar, FPropertyTagData tagData)
    {
        if (Ar.Ver < EUnrealEngineObjectUE4Version.PROPERTY_TAG_SET_MAP_SUPPORT)
        {
            if (Ar.Game == EGame.GAME_DaysGone)
                DaysGoneProperties.GetMapPropertyTypes(tagData.Name, out tagData.InnerType, out tagData.ValueType);              
        }

        if (tagData.InnerType == null || tagData.ValueType == null)
            throw new ParserException(Ar, "Can't serialize UScriptMap without key or value type");

        if (!Ar.HasUnversionedProperties && tagData.Name is not null && Ar.Versions.MapStructTypes.TryGetValue(tagData.Name, out var mapStructTypes))
        {
            if (!string.IsNullOrEmpty(mapStructTypes.Key)) tagData.InnerTypeData = new FPropertyTagData(mapStructTypes.Key);
            if (!string.IsNullOrEmpty(mapStructTypes.Value)) tagData.ValueTypeData = new FPropertyTagData(mapStructTypes.Value);
        }

        var numKeysToRemove = Ar.Read<int>();
        for (var i = 0; i < numKeysToRemove; i++)
        {
            FPropertyTagType.ReadPropertyTagType(Ar, tagData.InnerType, tagData.InnerTypeData, ReadType.MAP);
        }

        var numEntries = Ar.Read<int>();
        Properties = new Dictionary<FPropertyTagType, FPropertyTagType?>(numEntries);
        for (var i = 0; i < numEntries; i++)
        {
            var isReadingValue = false;
            try
            {
                var key = FPropertyTagType.ReadPropertyTagType(Ar, tagData.InnerType, tagData.InnerTypeData, ReadType.MAP);
                isReadingValue = true;
                var value = FPropertyTagType.ReadPropertyTagType(Ar, tagData.ValueType, tagData.ValueTypeData, ReadType.MAP);
                Properties[key ?? new StrProperty($"UNK_Entry_{i}")] = value;
            }
            catch (ParserException e)
            {
                throw new ParserException(Ar, $"Failed to read {(isReadingValue ? "value" : "key")} for index {i} in map", e);
            }
        }
    }
}
