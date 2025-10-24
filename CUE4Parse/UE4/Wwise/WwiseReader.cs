using System;
using System.Collections.Generic;
using System.Text;
using CUE4Parse.UE4.Exceptions;
using CUE4Parse.UE4.Objects.Core.Misc;
using CUE4Parse.UE4.Readers;
using CUE4Parse.UE4.Wwise.Enums;
using CUE4Parse.UE4.Wwise.Objects;
using CUE4Parse.UE4.Wwise.Objects.HIRC;
using CUE4Parse.Utils;
using Newtonsoft.Json;
using Serilog;

namespace CUE4Parse.UE4.Wwise;

public class RIFFSectionSizeException : Exception;

[JsonConverter(typeof(WwiseConverter))]
public class WwiseReader
{
    public readonly string Path;
    private uint Version => Header.Version;

    public BankHeader Header { get; }
    public AkFolder[]? Folders { get; }
    public Dictionary<uint, string>? AKPluginList { get; }
    public MediaHeader[]? WemIndexes { get; }
    public byte[][]? WemSounds { get; }
    public Hierarchy[]? Hierarchies { get; }
    public Dictionary<uint, string> BankIDToFileName { get; } = [];
    public string? Platform { get; }
    public Dictionary<string, byte[]> WwiseEncodedMedias { get; } = [];
    public GlobalSettings? GlobalSettings { get; }
    public EnvSettings? EnvSettings { get; }
    public byte[] WemFile { get; } = [];

    public WwiseReader(FArchive Ar)
    {
        Path = Ar.Name;
        while (Ar.Position < Ar.Length)
        {
            var sectionIdentifier = Ar.Read<ChunkID>();
            var sectionLength = Ar.Read<int>();
            var position = Ar.Position;

            switch (sectionIdentifier)
            {
                case ChunkID.AKPK:
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
                            folder.Entries[i] = entry;
                        }
                    }
                    break;
                case ChunkID.BankHeader:
                    Header = new BankHeader(Ar, sectionLength);
                    WwiseVersions.SetVersion(Version);
#if DEBUG
                    if (!WwiseVersions.IsSupported())
                        Log.Warning($"Wwise version {Version} is not supported");
#endif
                    break;
                case ChunkID.BankInit:
                    AKPluginList = Ar.ReadMap(Ar.Read<uint>, () => Version <= 136 ? Ar.ReadFString() : ReadStzString(Ar));
                    break;
                case ChunkID.BankDataIndex:
                    WemIndexes = Ar.ReadArray(sectionLength / 12, Ar.Read<MediaHeader>);
                    break;
                case ChunkID.BankData:
                    if (WemIndexes == null)
                        break;
                    WemSounds = new byte[WemIndexes.Length][];
                    for (var i = 0; i < WemSounds.Length; i++)
                    {
                        Ar.Position = position + WemIndexes[i].Offset;
                        WemSounds[i] = Ar.ReadBytes(WemIndexes[i].Size);
                        WwiseEncodedMedias[WemIndexes[i].Id.ToString()] = WemSounds[i];
                    }
                    break;
                case ChunkID.BankHierarchy:
                    Hierarchies = Ar.ReadArray(() => new Hierarchy(Ar));
                    break;
                case ChunkID.RIFF:
                    if (Ar.Position + sectionLength > Ar.Length)
                        throw new RIFFSectionSizeException();
                    Ar.Position -= 8;
                    var wemData = Ar.ReadBytes(8 + sectionLength);
                    WemFile = wemData;
                    break;
                case ChunkID.BankStrMap:
                    Ar.Position += 4;//var type = Ar.Read<AKBKStringType>;
                    BankIDToFileName = Ar.ReadMap(Ar.Read<uint>, Ar.ReadString);
                    break;
                case ChunkID.BankStateMg:
                    //if (WwiseVersions.IsSupported())
                    //{
                    //    GlobalSettings = new GlobalSettings(Ar);
                    //}
                    break;
                case ChunkID.BankEnvSetting:
                    if (WwiseVersions.IsSupported()) // Let's guard this just in case
                    {
                        EnvSettings = new EnvSettings(Ar);
                    }
                    break;
                case ChunkID.FXPR:
                    break;
                case ChunkID.BankCustomPlatformName:
                    Platform = Version <= 136 ? Encoding.ASCII.GetString(Ar.ReadArray<byte>()) : ReadStzString(Ar);
                    break;
                case ChunkID.PLUGIN:
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
                    WwiseEncodedMedias[BankIDToFileName.TryGetValue(entry.NameHash, out var k) ? k : $"{entry.Path.ToUpper()}_{entry.NameHash}"] = entry.Data;
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
            var sectionIdentifier = Ar.Read<ChunkID>();
            var sectionLength = Ar.Read<int>();
            var sectionStart = Ar.Position;

            if (sectionIdentifier == ChunkID.BankHeader)
            {
                Ar.Read<uint>(); // Version
                var soundBankId = Ar.Read<uint>();
                return soundBankId;
            }

            Ar.Position = sectionStart + sectionLength;
        }

        return null;
    }

    #region Readers
    public static string ReadStzString(FArchive Ar)
    {
        var bytes = new List<byte>(16);

        while (true)
        {
            var b = Ar.Read<byte>();
            if (b == 0) break;
            bytes.Add(b);

            if (bytes.Count >= 255)
                throw new ArgumentException("ReadStz: string too long (no terminator within 255 bytes).");
        }
        return Encoding.UTF8.GetString([.. bytes]);
    }

    public static int Read7BitEncodedIntBE(FArchive Ar)
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
    #endregion
}
