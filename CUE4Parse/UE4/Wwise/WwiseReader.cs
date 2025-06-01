using System;
using System.Collections.Generic;
using System.Text;
using CUE4Parse.UE4.Exceptions;
using CUE4Parse.UE4.Readers;
using CUE4Parse.UE4.Wwise.Enums;
using CUE4Parse.UE4.Wwise.Objects;
using CUE4Parse.UE4.Wwise.Objects.HIRC;
using Newtonsoft.Json;
using Serilog;

namespace CUE4Parse.UE4.Wwise;

[JsonConverter(typeof(WwiseConverter))]
public class WwiseReader
{
    public string Path { get; }
    public BankHeader Header { get; }
    public AkFolder[]? Folders { get; }
    public string[]? Initialization { get; }
    public DataIndex[]? WemIndexes { get; }
    public byte[][]? WemSounds { get; }
    public Hierarchy[]? Hierarchies { get; }
    public Dictionary<uint, string>? IdToString { get; } = [];
    public string? Platform { get; }
    public Dictionary<string, byte[]> WwiseEncodedMedias { get; } = [];
    public GlobalSettings? GlobalSettings { get; }
    public EnvSettings? EnvSettings { get; }
    private uint Version => Header.Version;

    public WwiseReader(FArchive Ar)
    {
        Path = Ar.Name;
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
                    Header = new BankHeader(Ar, sectionLength);
                    WwiseVersions.SetVersion(Version);
#if DEBUG
                    if (!WwiseVersions.IsSupported()) Log.Warning($"Wwise version {Version} is not supported");
#endif
                    break;
                case ESectionIdentifier.INIT:
                    Initialization = Ar.ReadArray(() =>
                    {
                        Ar.Position += 4;
                        return Version <= 136 ? Ar.ReadFString() : ReadStzString(Ar);
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
                    //if (WwiseVersions.IsSupported())
                    //{
                    //    GlobalSettings = new GlobalSettings(Ar);
                    //}
                    break;
                case ESectionIdentifier.ENVS:
                    if (WwiseVersions.IsSupported()) // Let's guard this just in case
                    {
                        EnvSettings = new EnvSettings(Ar);
                    }
                    break;
                case ESectionIdentifier.FXPR:
                    break;
                case ESectionIdentifier.PLAT:
                    Platform = Version <= 136 ? Ar.ReadFString() : ReadStzString(Ar);
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
                    if (entry.IsSoundBank || entry.Data == null)
                        continue;
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

    /// Reads only the SoundBankId from a .bnk file
    /// In order to quickly find the SoundBank without parsing the entire file
    public static uint? TryReadSoundBankId(FArchive Ar)
    {
        while (Ar.Position < Ar.Length)
        {
            var sectionIdentifier = Ar.Read<ESectionIdentifier>();
            var sectionLength = Ar.Read<int>();
            var sectionStart = Ar.Position;

            if (sectionIdentifier == ESectionIdentifier.BKHD)
            {
                var version = Ar.Read<uint>();
                var soundBankId = Ar.Read<uint>();
                return soundBankId;
            }

            Ar.Position = sectionStart + sectionLength;
        }

        return null;
    }

    public static string ReadStzString(FArchive Ar)
    {
        List<byte> bytes = [];
        int count = 0;

        while (true)
        {
            var b = Ar.Read<byte>();
            if (b == 0)
                break;
            bytes.Add(b);

            if (++count > 255)
                throw new ArgumentException("ReadStz: string too long (no terminator within 255 bytes).");
        }
        return Encoding.UTF8.GetString([.. bytes]);
    }

    public static int ReadBigEndianVarInt(FArchive Ar)
    {
        int max = 0;

        byte cur = Ar.Read<byte>();
        int value = cur & 0x7F;

        while ((cur & 0x80) != 0)
        {
            if (++max >= 10)
                throw new FormatException("Unexpected variable loop count");

            cur = Ar.Read<byte>();
            value = (value << 7) | (cur & 0x7F);
        }

        return value;
    }
}
