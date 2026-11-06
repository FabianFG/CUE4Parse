using CUE4Parse.GameTypes.AoC.Objects;
using CUE4Parse.GameTypes.DaysGone.Assets;
using CUE4Parse.GameTypes.SOD2.Assets;
using CUE4Parse.UE4.Assets.Objects.Properties;
using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Exceptions;
using CUE4Parse.UE4.Versions;
using Newtonsoft.Json;

namespace CUE4Parse.UE4.Assets.Objects;

[JsonConverter(typeof(UScriptMapConverter))]
public class UScriptMap
{
    public Dictionary<FPropertyTagType, FPropertyTagType?> Properties;
    // The keys a map saved against its defaults removes from them. Keys that can't be read are left out
    public IReadOnlyList<FPropertyTagType> KeysToRemove = [];
    // UE writes -1 as the removal count when the map replaces its defaults instead of patching them.
    // False doesn't always mean a patch: maps saved without defaults, such as maps inside arrays or map values,
    // come with no removals and all their entries, and UE empties the map before loading them
    public bool ReplacesDefaults;

    public UScriptMap() => Properties = [];

    public UScriptMap(Dictionary<FPropertyTagType, FPropertyTagType?> properties) => Properties = properties;

    public UScriptMap(FAssetArchive Ar, FPropertyTagData tagData, ReadType readType)
    {
        if (Ar.Ver < EUnrealEngineObjectUE4Version.PROPERTY_TAG_SET_MAP_SUPPORT)
        {
            (tagData.InnerType, tagData.ValueType) = Ar.Game switch
            {
                GAME_DaysGone => DaysGoneProperties.GetMapPropertyTypes(tagData.Name),
                GAME_StateOfDecay2 => SOD2Properties.GetMapPropertyTypes(tagData.Name),
                GAME_WeHappyFew => tagData.Name switch
                {
                    "PointMap" or "JunctionMap" or "RoadMap" => ("IntProperty", "StructProperty"),
                    "States" => ("NameProperty", "StructProperty"),
                    _ => (null, null)
                },
                _ => (null, null)
            };
        }

        if (tagData.InnerType == null || tagData.ValueType == null)
            throw new ParserException(Ar, "Can't serialize UScriptMap without key or value type");

        if (Ar.Game is GAME_DeadIsland2)
        {
            if (tagData.InnerType is "StructProperty") tagData.InnerTypeData = new FPropertyTag(Ar, false).TagData;
            if (tagData.ValueType is "StructProperty") tagData.ValueTypeData = new FPropertyTag(Ar, false).TagData;
        }

        if (!Ar.HasUnversionedProperties && tagData.Name is not null && Ar.Versions.MapStructTypes.TryGetValue(tagData.Name, out var mapStructTypes))
        {
            if (!string.IsNullOrEmpty(mapStructTypes.Key)) tagData.InnerTypeData = new FPropertyTagData(mapStructTypes.Key);
            if (!string.IsNullOrEmpty(mapStructTypes.Value)) tagData.ValueTypeData = new FPropertyTagData(mapStructTypes.Value);
        }

        if (readType != ReadType.RAW)
        {
            var numKeysToRemove = Ar.Read<int>();
            ReplacesDefaults = numKeysToRemove == -1;
            if (numKeysToRemove > 0)
            {
                EnsureCountFits(Ar, numKeysToRemove, "removed keys");
                var keysToRemove = new List<FPropertyTagType>(numKeysToRemove);
                for (var i = 0; i < numKeysToRemove; i++)
                {
                    if (FPropertyTagType.ReadPropertyTagType(Ar, tagData.InnerType, tagData.InnerTypeData, ReadType.MAP) is { } key)
                        keysToRemove.Add(key);
                }
                KeysToRemove = keysToRemove;
            }
        }

        if (Ar.Game is GAME_AshesOfCreation && Ar is FAoCDBCReader) Ar.Position += 4;

        var type = readType == ReadType.RAW ? ReadType.RAW : ReadType.MAP;
        var numEntries = Ar.Read<int>();
        EnsureCountFits(Ar, numEntries, "entries");
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
            catch (Exception e)
            {
                 throw new ParserException(Ar, $"Failed to read {(isReadingValue ? "value" : "key")} for index {i} in map", e);
            }
        }
    }

    // Every key, value or element takes at least one byte, so a larger count comes from a misread and would only allocate memory
    internal static void EnsureCountFits(FAssetArchive Ar, int count, string what)
    {
        if (count > Ar.Length - Ar.Position)
            throw new ParserException(Ar, $"{count} {what} don't fit in the remaining archive size {Ar.Length - Ar.Position}");
    }
}
