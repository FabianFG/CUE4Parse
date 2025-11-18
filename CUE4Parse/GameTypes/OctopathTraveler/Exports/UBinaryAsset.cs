using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CUE4Parse.MappingsProvider;
using CUE4Parse.UE4.Assets.Exports;
using CUE4Parse.UE4.Assets.Objects;
using CUE4Parse.UE4.Assets.Objects.Properties;
using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Exceptions;
using CUE4Parse.UE4.Objects.UObject;
using CUE4Parse.UE4.Readers;
using CUE4Parse.UE4.Versions;
using Serilog;

namespace CUE4Parse.GameTypes.OctopathTraveler.Exports;

public class UBinaryAsset : UObject
{
    public override void Deserialize(FAssetArchive Ar, long validPos)
    {
        base.Deserialize(Ar, validPos);

        if (Ar.Game != EGame.GAME_OctopathTraveler0) return;

        try
        {
            var data = GetOrDefault<byte[]>("BinaryData", []);
            using var dataAr = new FByteArchive("BinaryData", data, Ar.Versions);
            var name = Name;
            if (name.StartsWith("VillageBuildingFillMap"))
            {
                var str = new FPropertyTag
                {
                    Name = "Data",
                    PropertyType = "StrProperty",
                    Tag = new StrProperty(Encoding.UTF8.GetString(data)),
                    TagData = new FPropertyTagData(new PropertyType("StrProperty")),
                };
                Properties.Clear();
                Properties.Add(str);
                return;
            }

            if (name.StartsWith("BattlePlaybackData")) name = "BattlePlaybacks";
            if (name.StartsWith("NpcTalkList")) name = "FieldTalkList";
            if (name.StartsWith("NpcSetList")) name = "NpcSetList";
            if (name.StartsWith("_bytesDP")) name = "NpcSetList";
            if (name.StartsWith("GameText")) name = "GameTextID";
            if (name.StartsWith("EvTutorial") || name.StartsWith("EvTownArrival") || name.StartsWith("EvSml")
                || name.StartsWith("EvMap") || name.StartsWith("EvPChatOth") || name.StartsWith("EvQuSQ")
                || name.StartsWith("EvB") || name.StartsWith("EvJ") || name.StartsWith("EvT"))
                name = "EventParam";
            name = Name switch
            {
                //"DebugSizzlingGroup" => "DataID",
                "CharaReplaceList" => "CharacterReplaceList",
                "CharaTexID" or "EnemyTexID" or "TextureID" => "ResourceListID",
                "EventFinishProcessList" => "EventFinishProcess",
                "EventFlagVariableList" => "EventFlagVariable",
                "GameTextGraphic" or "GameTextPartVoice" or "GameTextEvent" => "GameTextEventID",
                "GameTextNPC" => "GameTextNPCID",
                "ItemClassupList" or "ItemTowerRecover" or "MapIconType" => "ItemList",
                "MapListTable" or "MapPathListTable" => "MapList",
                "NpcTalkList" => "NpcSetList",
                "SkillCalcType" => "SpecialSkillID",
                "SkillAvailMagnificationList" => "SkillAvailMagnificationConditionList",
                "TextFilter" => "TextFilterList",
                "VillageBuildingGradeUpRequire" => "VillageBuildingResourceDestruct",
                _ => name,
            };

            var tag = new FPropertyTag
            {
                Name = "Data",
                PropertyType = "StructProperty",
                Tag = ReadOctopathPropertyTagType(dataAr, Ar.Owner!.Mappings!, "StructProperty", new FPropertyTagData(name)),
                TagData = new FPropertyTagData(name),
            };
            if (dataAr.Position != dataAr.Length)
                Log.Warning("Did not read the full UBinaryAsset data for {0}, read {1} of {2} bytes", Name, dataAr.Position, dataAr.Length);

            Properties.Clear();
            Properties.Add(tag);
        }
        catch (Exception e)
        {
            Log.Error(e,"Failed to parse OctopathTraveler0 UBinaryAsset {0}", Name);
        }
    }

    public static FPropertyTagType? ReadOctopathPropertyTagType(FArchive Ar, TypeMappings mappings, string? propertyType, FPropertyTagData? tagData)
    {
        return propertyType switch
        {
            "IntProperty" => new IntProperty(ReadIntValue(Ar)),
            "StructProperty" => new StructProperty(ReadStruct(Ar, mappings, tagData?.StructType)),
            "ArrayProperty" => new ArrayProperty(ReadArray(Ar, mappings, tagData)),
            "StrProperty" => new StrProperty(ReadString(Ar)),
            "NameProperty" => new NameProperty(ReadString(Ar)),
            "FloatProperty" => new FloatProperty(ReadFloatValue(Ar)),
            "BoolProperty" => new BoolProperty((Ar.Read<byte>() & 1) == 1), // 0xc2 or 0xc3, but we need only 1 bit
            _ => null,
        };

        float ReadFloatValue(FArchive Ar) => BitConverter.ToSingle(Ar.ReadBytes(5).Reverse().ToArray());
        int ReadIntValue(FArchive Ar) => new FOctoInt(Ar).Value;

        UScriptArray ReadArray(FArchive Ar, TypeMappings mappings, FPropertyTagData? tagData)
        {
            var pos = Ar.Position;
            var header = new FOctoStructHeader(Ar);
            if (!header.Flags.HasFlag(FOctoStructHeader.TypeFlags.IsArray))
                throw new ParserException(Ar, $"Unknown ArrayProperty flags: {header.Flags}");

            var properties = new List<FPropertyTagType>(header.Length);
            for (int i = 0; i < header.Length; i++)
            {
                properties.Add(ReadOctopathPropertyTagType(Ar, mappings, tagData?.InnerType, tagData?.InnerTypeData));
            }

            return new UScriptArray(properties, tagData?.InnerType, tagData?.InnerTypeData);
        }

        FScriptStruct ReadStruct(FArchive Ar, TypeMappings mappings, string? structName)
        {
            mappings.Types.TryGetValue(structName, out var propMappings);
            if (propMappings == null)
            {
                throw new ParserException(Ar, $"No property mappings found for struct {structName}");
            }
            var properties = new List<FPropertyTag>();
            var header = new FOctoStructHeader(Ar);
            var propindex = 0;

            for (int i = 0; i < header.Length; i++)
            {
                var name = ReadString(Ar);
                if (!(propMappings.Properties.Values.FirstOrDefault(x => x.Name.Equals(name, StringComparison.OrdinalIgnoreCase)) is { } info)
                    && !AlternateOctopathPropertyLookup.Value.TryGetValue(name, out info))
                {
                    throw new ParserException(Ar, $"Unknown property with name {name}. Can't proceed with serialization (Serialized {properties.Count} properties until now)");
                }

                var tag = new FPropertyTag
                {
                    Name = new FName(info.Name),
                    PropertyType = new FName(info.MappingType.Type),
                    ArrayIndex = info.Index,
                    ArraySize = info.ArraySize,
                    TagData = new FPropertyTagData(info.MappingType),
                };

                var pos = Ar.Position;
                propindex++;
                try
                {
                    tag.Tag = ReadOctopathPropertyTagType(Ar, mappings, tag.PropertyType.Text, tag.TagData);
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
            var res = new FStructFallback();
            res.Properties.AddRange(properties);
            return new FScriptStruct(res);
        }
    }

    private static readonly Lazy<Dictionary<string, PropertyInfo>> AlternateOctopathPropertyLookup = new(() => new()
    {
        { "m_ChainConditionID", new PropertyInfo(-1, "m_ChainConditionID", new PropertyType("ArrayProperty", null, new PropertyType("IntProperty")), 1)},
        { "m_Inflence", new PropertyInfo(-1, "m_Inflence", new PropertyType("ArrayProperty", null, new PropertyType("IntProperty")), 1)},
        { "m_ChoiceJump", new PropertyInfo(-1, "m_ChoiceJump", new PropertyType("ArrayProperty", null, new PropertyType("IntProperty")), 1)},
        { "m_Root", new PropertyInfo(-1, "m_Root", new PropertyType("BoolProperty"), 1)},
        { "m_ChoiceTalk", new PropertyInfo(-1, "m_ChoiceTalk", new PropertyType("BoolProperty"), 1)},
        { "m_Affection", new PropertyInfo(-1, "m_Affection", new PropertyType("BoolProperty"), 1)},
        { "m_HardDungeonPairId", new PropertyInfo(-1, "m_HardDungeonPairId", new PropertyType("IntProperty"), 1) },
        { "m_HardDungeonPairNum", new PropertyInfo(-1, "m_HardDungeonPairNum", new PropertyType("IntProperty"), 1)},
        { "m_Hard", new PropertyInfo(-1, "m_Hard", new PropertyType("IntProperty"), 1)},
        { "m_CorrelationSpawnTrigger", new PropertyInfo(-1, "m_CorrelationSpawnTrigger", new PropertyType("IntProperty"), 1)},
        { "m_nLine", new PropertyInfo(-1, "m_nLine", new PropertyType("IntProperty"), 1)},
        { "m_nLineA", new PropertyInfo(-1, "m_nLineA", new PropertyType("IntProperty"), 1)},
        { "m_nLen", new PropertyInfo(-1, "m_nLen", new PropertyType("IntProperty"), 1)},
        { "m_nLenA", new PropertyInfo(-1, "m_nLenA", new PropertyType("IntProperty"), 1)},
        { "m_OwnerChara", new PropertyInfo(-1, "m_OwnerChara", new PropertyType("IntProperty"), 1)},
        { "m_Priority", new PropertyInfo(-1, "m_Priority", new PropertyType("IntProperty"), 1)},
        { "m_InputCheck", new PropertyInfo(-1, "m_InputCheck", new PropertyType("IntProperty"), 1)},
        { "m_Boss", new PropertyInfo(-1, "m_Boss", new PropertyType("IntProperty"), 1)},
        { "m_Progress", new PropertyInfo(-1, "m_Progress", new PropertyType("IntProperty"), 1)},
        { "m_TalkDir", new PropertyInfo(-1, "m_TalkDir", new PropertyType("IntProperty"), 1)},
        { "m_CategoryID", new PropertyInfo(-1, "m_CategoryID", new PropertyType("IntProperty"), 1)},
        { "m_EventID", new PropertyInfo(-1, "m_EventID", new PropertyType("IntProperty"), 1)},
        { "m_QuestID", new PropertyInfo(-1, "m_QuestID", new PropertyType("IntProperty"), 1)},
        { "m_0", new PropertyInfo(-1, "m_0", new PropertyType("IntProperty"), 1)},
        { "m_ChainTalk", new PropertyInfo(-1, "m_ChainTalk", new PropertyType("IntProperty"), 1)},
        { "m_TalkText", new PropertyInfo(-1, "m_TalkText", new PropertyType("IntProperty"), 1)},
        { "m_EmbedWord1", new PropertyInfo(-1, "m_EmbedWord1", new PropertyType("IntProperty"), 1)},
        { "m_EmbedWord2", new PropertyInfo(-1, "m_EmbedWord2", new PropertyType("IntProperty"), 1)},
        { "m_EmbedWord3", new PropertyInfo(-1, "m_EmbedWord3", new PropertyType("IntProperty"), 1)},
        { "m_EmbedWord4", new PropertyInfo(-1, "m_EmbedWord4", new PropertyType("IntProperty"), 1)},
        { "m_Params", new PropertyInfo(-1, "m_Params", new PropertyType("StrProperty"), 1)},
        { "m", new PropertyInfo(-1, "m", new PropertyType("StrProperty"), 1)},
        { "m_EnemySkillID_01", new PropertyInfo(-1, "m_EnemySkillID_01", new PropertyType("StrProperty"), 1)},
    });

    public static string ReadString(FArchive Ar)
    {
        var nameHeader = new FOctoStructHeader(Ar);
        return Encoding.UTF8.GetString(Ar.ReadBytes(nameHeader.Length));
    }

    public struct FOctoInt
    {
        public int Value;
        public FOctoInt(FArchive Ar)
        {
            var header = Ar.Read<byte>();
            if (header >> 7 == 0)
            {
                Value = header & 0x7f;
                return;
            }

            var lower = header & 0xf;
            Value = (header >> 4) switch
            {
                12 => Ar.Read<byte>(),
                13 when lower == 1 => BinaryPrimitives.ReverseEndianness(Ar.Read<short>()),
                13 when lower == 2 => BinaryPrimitives.ReverseEndianness(Ar.Read<int>()),
                14 or 15 => lower,
                _ => throw new ParserException("Unknow int header type"),
            };
        }
    }

    public struct FOctoStructHeader
    {
        public int Length;
        public TypeFlags Flags;
        public byte SomeData;

        public FOctoStructHeader(FArchive Ar)
        {
            var header = Ar.Read<byte>();
            Flags = (TypeFlags) (header >> 4);
            Length = Flags.HasFlag(TypeFlags.ExtendedLength) ?  BinaryPrimitives.ReverseEndianness(Ar.Read<ushort>()) : (header & 0xf);
        }

        [Flags]
        public enum TypeFlags : byte
        {
            None = 0,
            IsArray = 1 << 0,
            IsString = 1 << 1,
            ExtendedLength = 1 << 2,
            Unknown = 1 << 3,
        }
    }
}
