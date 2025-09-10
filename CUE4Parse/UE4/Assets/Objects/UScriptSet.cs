using System.Collections.Generic;
using CUE4Parse.GameTypes.DuneAwakening.Assets.Objects;
using CUE4Parse.UE4.Assets.Objects.Properties;
using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Exceptions;
using CUE4Parse.UE4.Versions;
using Newtonsoft.Json;
using Serilog;

namespace CUE4Parse.UE4.Assets.Objects;

[JsonConverter(typeof(UScriptSetConverter))]
public class UScriptSet
{
    public readonly List<FPropertyTagType> Properties;

    public UScriptSet()
    {
        Properties = [];
    }

    public UScriptSet(FAssetArchive Ar, FPropertyTagData? tagData)
    {
        if (Ar.Game == EGame.GAME_StateOfDecay2 && tagData is not null)
        {
            tagData.InnerType = tagData.Name switch
            {
                "AllEntityIds" or "SceneNameSet" => "NameProperty",
                "TextVarSources" => "StrProperty",
                _ => null
            };
        }

        var innerType = tagData?.InnerType ?? throw new ParserException(Ar, "UScriptSet needs inner type");

        if (tagData.InnerTypeData is null && !Ar.HasUnversionedProperties && innerType == "StructProperty")
        {
            if (tagData.Name is "AnimSequenceInstances" or "PostProcessInstances")
            {
                tagData.InnerTypeData = new FPropertyTagData("Guid");
            }

            tagData.InnerTypeData = Ar.Game switch
            {
                EGame.GAME_ThroneAndLiberty when tagData.Name is "ExcludeMeshes" or "IncludeMeshes" => new FPropertyTagData("SoftObjectPath"),
                EGame.GAME_MetroAwakening when tagData.Name is "SoundscapePaletteCollection" => new FPropertyTagData("SoftObjectPath"),
                EGame.GAME_Avowed when tagData.Name.EndsWith("IDs") => new FPropertyTagData("Guid"),
                EGame.GAME_DuneAwakening => DAStructs.ResolveSetPropertyInnerTypeData(tagData),
                _ => tagData.InnerTypeData
            };
        }

        var numElementsToRemove = Ar.Read<int>();
        for (var i = 0; i < numElementsToRemove; i++)
        {
            FPropertyTagType.ReadPropertyTagType(Ar, innerType, tagData.InnerTypeData, ReadType.ARRAY);
        }

        var num = Ar.Read<int>();
        Properties = new List<FPropertyTagType>(num);
        for (var i = 0; i < num; i++)
        {
            var property = FPropertyTagType.ReadPropertyTagType(Ar, innerType, tagData.InnerTypeData, ReadType.ARRAY);
            if (property != null)
                Properties.Add(property);
            else
                Log.Debug($"Failed to read element for index {i} in set");
        }
    }
}
