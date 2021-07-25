using System;
using System.Collections.Generic;
using CUE4Parse.UE4.Exceptions;
using CUE4Parse.UE4.Readers;
using CUE4Parse.UE4.Wwise.Enums;
using CUE4Parse.UE4.Wwise.Objects;
using Newtonsoft.Json;
using Serilog;

namespace CUE4Parse.UE4.Wwise
{
    [JsonConverter(typeof(WwiseConverter))]
    public class WwiseReader
    {
        public BankHeader Header { get; }
        public AkFolder[]? Folders { get; }
        public string[]? Initialization { get; }
        public DataIndex[]? WemIndexes { get; }
        public byte[][]? WemSounds { get; }
        public Hierarchy[]? Hierarchy { get; }
        public Dictionary<uint, string>? IdToString { get; }
        public string? Platform { get; }
        public Dictionary<string, byte[]> WwiseEncodedMedias { get; }
        
        public WwiseReader(FArchive Ar)
        {
            IdToString = new Dictionary<uint, string>();
            WwiseEncodedMedias = new Dictionary<string, byte[]>();
            while (Ar.Position < Ar.Length)
            {
                var sectionIdentifier = Ar.Read<ESectionIdentifier>();
                var sectionLength = Ar.Read<int>();
                var position = Ar.Position;
                
                switch (sectionIdentifier)
                {
                    case ESectionIdentifier.AKPK:
                        if (!Ar.ReadBoolean())
                            throw new ParserException(Ar, $"'{Ar.Name}' has unsupported endianness.");
                        
                        Ar.Position += 16;
                        Folders = Ar.ReadArray(() => new AkFolder(Ar));
                        foreach (var folder in Folders) folder.PopulateName(Ar);
                        foreach (var folder in Folders)
                        {
                            folder.Entries = new AkEntry[Ar.Read<uint>()];
                            for (var i = 0; i < folder.Entries.Length; i++)
                            {
                                var entry = new AkEntry(Ar);
                                entry.Path = Folders[entry.FolderId].Name;
                                
                                var savePos = Ar.Position;
                                Ar.Position = entry.Offset;
                                entry.IsSoundBank = Ar.Read<ESectionIdentifier>() == ESectionIdentifier.BKHD;
                                Ar.Position -= 4;
                                entry.Data = Ar.ReadBytes(entry.Size);
                                Ar.Position = savePos;

                                folder.Entries[i] = entry;
                            }
                        }
                        break;
                    case ESectionIdentifier.BKHD:
                        Header = Ar.Read<BankHeader>();
                        break;
                    case ESectionIdentifier.INIT:
                        Initialization = Ar.ReadArray(() =>
                        {
                            Ar.Position += 4;
                            return Ar.ReadFString();
                        });
                        break;
                    case ESectionIdentifier.DIDX:
                        WemIndexes = Ar.ReadArray(sectionLength / 12, Ar.Read<DataIndex>);
                        break;
                    case ESectionIdentifier.DATA:
                        if (WemIndexes == null) break;
                        WemSounds = new byte[WemIndexes.Length][];
                        for (var i = 0; i < WemSounds.Length; i++)
                        {
                            Ar.Position = position + WemIndexes[i].Offset;
                            WemSounds[i] = Ar.ReadBytes(WemIndexes[i].Length);
                            WwiseEncodedMedias[WemIndexes[i].Id.ToString()] = WemSounds[i];
                        }
                        break;
                    case ESectionIdentifier.HIRC:
                        Hierarchy = Ar.ReadArray(() => new Hierarchy(Ar));
                        break;
                    case ESectionIdentifier.RIFF:
                        // read byte[sectionLength] it's simply a wem file
                        break;
                    case ESectionIdentifier.STID:
                        Ar.Position += 4;
                        var count = Ar.Read<int>();
                        for (var i = 0; i < count; i++)
                        {
                            IdToString[Ar.Read<uint>()] = Ar.ReadString();
                        }
                        break;
                    case ESectionIdentifier.STMG:
                        break;
                    case ESectionIdentifier.ENVS:
                        break;
                    case ESectionIdentifier.FXPR:
                        break;
                    case ESectionIdentifier.PLAT:
                        Platform = Ar.ReadFString();
                        break;
                    default:
#if DEBUG
                        Log.Warning($"Unknown section {sectionIdentifier:X} at {position - sizeof(uint) - sizeof(uint)}");
#endif
                        break;
                }
                
                if (Ar.Position != position + sectionLength)
                {
                    var shouldBe = position + sectionLength;
#if DEBUG
                    Log.Warning($"Didn't read 0x{sectionIdentifier:X} correctly (at {Ar.Position}, should be {shouldBe})");
#endif
                    Ar.Position = shouldBe;
                }
            }

            
            if (Folders != null)
            {
                foreach (var folder in Folders)
                {
                    foreach (var entry in folder.Entries)
                    {
                        if (entry.IsSoundBank || entry.Data == null) continue;
                        WwiseEncodedMedias[IdToString.TryGetValue(entry.NameHash, out var k) ? k : $"{entry.Path.ToUpper()}_{entry.NameHash}"] = entry.Data;
                    }
                }
            }
        }
    }
    
    public class WwiseConverter : JsonConverter<WwiseReader>
    {
        public override void WriteJson(JsonWriter writer, WwiseReader value, JsonSerializer serializer)
        {
            writer.WriteStartObject();
            
            writer.WritePropertyName("Header");
            serializer.Serialize(writer, value.Header);
            
            writer.WritePropertyName("Folders");
            serializer.Serialize(writer, value.Folders);

            writer.WritePropertyName("Initialization");
            serializer.Serialize(writer, value.Initialization);
            
            writer.WritePropertyName("WemIndexes");
            serializer.Serialize(writer, value.WemIndexes);
            
            writer.WritePropertyName("Hierarchy");
            serializer.Serialize(writer, value.Hierarchy);
            
            writer.WritePropertyName("IdToString");
            serializer.Serialize(writer, value.IdToString);
            
            writer.WritePropertyName("Platform");
            writer.WriteValue(value.Platform);
            
            writer.WriteEndObject();
        }

        public override WwiseReader ReadJson(JsonReader reader, Type objectType, WwiseReader existingValue, bool hasExistingValue,
            JsonSerializer serializer)
        {
            throw new NotImplementedException();
        }
    }
}