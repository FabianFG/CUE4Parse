using System.Collections.Generic;
using System.Text;
using CUE4Parse.UE4.Exceptions;
using CUE4Parse.UE4.Readers;
using CUE4Parse.UE4.Wwise.Enums;
using CUE4Parse.UE4.Wwise.Objects;
using Newtonsoft.Json;
using Serilog;

namespace CUE4Parse.UE4.Wwise;

[JsonConverter(typeof(WwiseConverter))]
public class WwiseReader
{
    public BankHeader Header { get; }
    public AkFolder[]? Folders { get; }
    public string[]? Initialization { get; }
    public DataIndex[]? WemIndexes { get; }
    public byte[][]? WemSounds { get; }
    public Hierarchy[]? Hierarchies { get; }
    public Dictionary<uint, string>? IdToString { get; }
    public string? Platform { get; }
    public Dictionary<string, byte[]> WwiseEncodedMedias { get; }
    private uint Version => Header.Version;

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
                        return Version <= 136 ? Ar.ReadFString() : ReadString(Ar);
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
                    Hierarchies = Ar.ReadArray(() => new Hierarchy(Ar));
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
                    Platform = Version <= 136 ? Ar.ReadFString() : ReadString(Ar);
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
        if (Hierarchies != null)
        {
            // the proper way seems to read the header id to get the main hierarchy
            // that hierarchy will give other hierarchy ids and so on until the end sound data
            // but not everything is currently getting parsed so that's not possible

            // foreach (var hierarchy in Hierarchies)
            // {
            //     switch (hierarchy.Type)
            //     {
            //         case EHierarchyObjectType.SoundSfxVoice when hierarchy.Data is HierarchySoundSfxVoice
            //         {
            //             SoundSource: ESoundSource.Embedded
            //         } sfxVoice:
            //             WwiseEncodedMedias[IdToString.TryGetValue(sfxVoice.SourceId, out var k) ? k : $"{sfxVoice.SourceId}"] = null;
            //             break;
            //         default:
            //             break;
            //     }
            // }
        }
    }

    public string ReadString(FArchive Ar)
    {
        List<byte> bytes = [];

        while (true)
        {
            var b = Ar.Read<byte>();
            if (b == 0)
                break;
            bytes.Add(b);
        }
        return Encoding.UTF8.GetString(bytes.ToArray());
    }
}
