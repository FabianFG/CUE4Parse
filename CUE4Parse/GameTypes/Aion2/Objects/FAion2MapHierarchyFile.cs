using System;
using CUE4Parse.FileProvider.Objects;
using CUE4Parse.UE4.Exceptions;
using CUE4Parse.UE4.Readers;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace CUE4Parse.GameTypes.Aion2.Objects;

[JsonConverter(typeof(FAion2MapHierarchyFileConverter))]
public class FAion2MapHierarchyFile : FAion2DataFile
{
    public FAion2World[] Worlds;

    public FAion2MapHierarchyFile(GameFile file)
    {
        using var Ar = file.SafeCreateReader();
        if (Ar is null) return;

        Version = Ar.Read<int>();
        Worlds = Ar.ReadArray(() => new FAion2World(Ar));
    }

    public class FAion2World
    {
        public uint Id;
        public FAion2SubZone[] SubZones;

        public FAion2World(FArchive Ar)
        {
            var id = Ar.Read<uint>();
            var type = Ar.Read<EAionMapDataHierarchy>();
            
            if (type != EAionMapDataHierarchy.Map)
                throw new ParserException(Ar, $"Expected Subzone type, got {type} type");
            Id = Ar.Read<uint>();
            if (Id != id)
                throw new ParserException(Ar, $"World ID mismatch, expected {Id}, got {Id}");
            SubZones = Ar.ReadArray(() => new FAion2SubZone(Ar));
        }
    }

    public class FAion2SubZone
    {
        public uint Id;
        public FAion2NPCType[] NPCTypes;

        public FAion2SubZone(FArchive Ar)
        {
            var type = Ar.Read<EAionMapDataHierarchy>();
            if (type != EAionMapDataHierarchy.Subzone)
                throw new ParserException(Ar, $"Expected Subzone type, got {type} type");
            Id = Ar.Read<uint>();
            NPCTypes = Ar.ReadArray(() => new FAion2NPCType(Ar));
        }
    }

    public class FAion2NPCType
    {
        [JsonConverter(typeof(StringEnumConverter))]
        public ENpcType Id;
        public FAion2NPC[] NPCs;

        public FAion2NPCType(FArchive Ar)
        {
            var type = Ar.Read<EAionMapDataHierarchy>();
            if (type != EAionMapDataHierarchy.NPCType)
                throw new ParserException(Ar, $"Expected NPCType type, got {type} type");
            Id = Ar.Read<ENpcType>();
            NPCs = Ar.ReadArray(() => new FAion2NPC(Ar));
        }
    }

    public class FAion2NPC
    {
        public uint Id;
        public FAion2Item[] Items;

        public FAion2NPC(FArchive Ar)
        {
            var type = Ar.Read<EAionMapDataHierarchy>();
            if (type != EAionMapDataHierarchy.NPC)
                throw new ParserException(Ar, $"Expected NPC type, got {type} type");
            Id = Ar.Read<uint>();
            Items = Ar.ReadArray(() => new FAion2Item(Ar));
        }
    }

    public struct FAion2Item
    {
        public uint Id;

        public FAion2Item(FArchive Ar)
        {
            var type = Ar.Read<EAionMapDataHierarchy>();
            if (type != EAionMapDataHierarchy.Item)
                throw new ParserException(Ar, $"Expected Item type, got {type} type");
            Id = Ar.Read<uint>();
            Ar.Position += 4;
        }
    }

    public enum ENpcType : int
    {
        None = 0,
        Monster = 1,
        Citizen = 2,
        Etc = 3,
        Summon = 4,
        Bot = 5,
        Max = 6
    }

    public enum EAionMapDataHierarchy : byte
    {
        None = 0,
        Map = 1,
        Subzone = 2,
        NPCType = 3,
        NPC = 4,
        Item = 5,
        Max = 6
    }
}

public class FAion2MapHierarchyFileConverter : JsonConverter<FAion2MapHierarchyFile>
{
    public override FAion2MapHierarchyFile? ReadJson(JsonReader reader, Type objectType, FAion2MapHierarchyFile? existingValue, bool hasExistingValue, JsonSerializer serializer)
    {
        throw new NotImplementedException();
    }
    public override void WriteJson(JsonWriter writer, FAion2MapHierarchyFile? value, JsonSerializer serializer)
    {
        writer.WriteStartObject();
        writer.WritePropertyName(nameof(value.Version));
        serializer.Serialize(writer, value.Version);
        writer.WritePropertyName(nameof(value.Worlds));
        serializer.Serialize(writer, value.Worlds);
        writer.WriteEndObject();
    }
}
