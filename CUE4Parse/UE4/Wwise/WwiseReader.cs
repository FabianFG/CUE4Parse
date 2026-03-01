using System;
using System.Collections.Generic;
using System.Text;
using CUE4Parse.FileProvider.Objects;
using CUE4Parse.UE4.Assets.Objects;
using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Exceptions;
using CUE4Parse.UE4.Readers;
using CUE4Parse.UE4.Wwise.Enums;
using CUE4Parse.UE4.Wwise.Objects;
using CUE4Parse.UE4.Wwise.Objects.HIRC;
using Newtonsoft.Json;
using Serilog;

namespace CUE4Parse.UE4.Wwise;

public class RIFFSectionSizeException : Exception;

public abstract record WwiseDataSource;
public sealed record WwiseArchiveSource : WwiseDataSource;
public sealed record WwiseGameFileSource(GameFile File) : WwiseDataSource;
public sealed record WwiseBulkDataSource(FAssetArchive AssetAr, FByteBulkDataHeader Header) : WwiseDataSource;

[JsonConverter(typeof(WwiseConverter))]
public class WwiseReader
{
    public readonly string Path;
    private uint Version => Header.Version;
    private readonly WwiseDataSource? _source;

    public AkBankHeader Header { get; }
    public AkFolder[]? Folders { get; }
    public Dictionary<uint, string>? AKPluginList { get; }
    public MediaHeader[]? WemIndexes { get; }
    public FDeferredByteData?[]? WemSounds { get; }
    public Hierarchy[]? Hierarchies { get; }
    public Dictionary<uint, string> BankIDToFileName { get; } = [];
    public string? Platform { get; }
    public Dictionary<string, FDeferredByteData> WwiseEncodedMedias { get; } = [];
    public GlobalSettings? GlobalSettings { get; }
    public CAkEnvironmentsMgr? EnvSettings { get; }
    public FDeferredByteData? WemFile { get; }
    public FDeferredByteData? PluginData { get; }
    public FDeferredByteData? MidiData { get; }
    public long LoadedSize { get; }
    public long TotalSize { get; }

    public WwiseReader(FArchive Ar, WwiseDataSource source)
    {
        Path = Ar.Name;
        _source = source;
        TotalSize = Ar.Length;

        while (Ar.Position < Ar.Length)
        {
            var sectionIdentifier = Ar.Read<EChunkID>();
            var sectionLength = Ar.Read<int>();
            var position = Ar.Position;

            switch (sectionIdentifier)
            {
                case EChunkID.AKPK:
                    LoadedSize += sectionLength;
                    if (!Ar.ReadBoolean())
                        throw new ParserException(Ar, $"'{Ar.Name}' has unsupported endianness.");

                    long entriesOffset = Ar.Read<int>();
                    Ar.Position += 12;
                    var namesOffset = Ar.Position;
                    entriesOffset += namesOffset + sizeof(int);
                    Folders = Ar.ReadArray(() => new AkFolder(Ar));
                    foreach (var folder in Folders)
                        folder.PopulateName(Ar, namesOffset);
                    Ar.Position = entriesOffset;
                    // To-Do : rewrite with FDefferedByteData and correct offset multiplier
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
                case EChunkID.BankHeader:
                    LoadedSize += sectionLength;
                    Header = new AkBankHeader(Ar, sectionLength);
                    WwiseVersions.SetVersion(Version);
                    if (!WwiseVersions.IsSupported())
                        Log.Warning($"Wwise version {Version} is not supported");
                    break;
                case EChunkID.BankInit:
                    LoadedSize += sectionLength;
                    AKPluginList = Ar.ReadMap(Ar.Read<uint>, () => Version <= 136 ? Ar.ReadFString() : ReadStzString(Ar));
                    break;
                case EChunkID.BankDataIndex:
                    LoadedSize += sectionLength;
                    WemIndexes = Ar.ReadArray(sectionLength / 12, Ar.Read<MediaHeader>);
                    break;
                case EChunkID.BankData:
                    if (WemIndexes == null)
                        break;

                    WemSounds = new FDeferredByteData[WemIndexes.Length];
                    for (var i = 0; i < WemSounds.Length; i++)
                    {
                        var temp = ReadDeferredByteData(Ar, _source, position + WemIndexes[i].Offset, WemIndexes[i].Size);
                        LoadedSize += temp.LoadedSize;
                        WwiseEncodedMedias[WemIndexes[i].Id.ToString()] = temp;
                    }
                    break;
                case EChunkID.BankHierarchy:
                    LoadedSize += sectionLength;
                    Hierarchies = Ar.ReadArray(() => new Hierarchy(Ar));
                    break;
                case EChunkID.RIFF:
                    if (Ar.Position + sectionLength > Ar.Length)
                        throw new RIFFSectionSizeException();
                    Ar.Position -= 8;
                    WemFile = ReadDeferredByteData(Ar, _source, Ar.Position, 8 + sectionLength);
                    LoadedSize += WemFile.LoadedSize;
                    break;
                case EChunkID.BankStrMap:
                    LoadedSize += sectionLength;
                    Ar.Position += 4; //var type = Ar.Read<AKBKStringType>;
                    BankIDToFileName = Ar.ReadMap(Ar.Read<uint>, Ar.ReadString);
                    break;
                case EChunkID.BankStateMg:
                    if (WwiseVersions.IsSupported())
                    {
                        LoadedSize += sectionLength;
                        GlobalSettings = new GlobalSettings(Ar);
                    }
                    break;
                case EChunkID.BankEnvSetting:
                    if (WwiseVersions.IsSupported()) // Let's guard this just in case
                    {
                        LoadedSize += sectionLength;
                        EnvSettings = new CAkEnvironmentsMgr(Ar);
                    }
                    break;
                case EChunkID.FXPR:
                    break;
                case EChunkID.BankCustomPlatformName:
                    LoadedSize += sectionLength;
                    Platform = Version <= 136 ? Encoding.ASCII.GetString(Ar.ReadArray<byte>()).TrimEnd('\0') : ReadStzString(Ar);
                    break;
                case EChunkID.PLUGIN:
                    Ar.Position -= 8;
                    PluginData = ReadDeferredByteData(Ar, _source, Ar.Position, 8 + sectionLength);
                    LoadedSize += PluginData.LoadedSize;
                    break;
                case EChunkID.MIDI:
                    Ar.Position -= 8;
                    MidiData = ReadDeferredByteData(Ar, _source, Ar.Position, 8 + sectionLength);
                    LoadedSize += MidiData.LoadedSize;
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
                Log.Warning($"Didn't read {sectionIdentifier} correctly (at {Ar.Position}, should be {shouldBe})");
#endif
                Ar.Position = shouldBe;
            }
        }

        if (Folders is null) return;

        foreach (var folder in Folders)
        {
            foreach (var entry in folder.Entries)
            {
                if (entry.IsSoundBank || entry.Data == null)
                    continue;

                WwiseEncodedMedias[entry.NameHash.ToString()] = new FDeferredByteData(entry.Data);
            }
        }
    }

    public FDeferredByteData ReadDeferredByteData(FArchive Ar, WwiseDataSource source, long offset, int size)
    {
        switch (source)
        {
            case WwiseBulkDataSource bulkDataSource when Ar.SupportPartialReads && bulkDataSource.AssetAr is { } assetAr && bulkDataSource.Header is { } head:
                var bulk = new FDeferredByteData(assetAr, head, offset, size);
                //if (head.BulkDataFlags is EBulkDataFlags.BULKDATA_None or EBulkDataFlags.BULKDATA_LazyLoadable)
                Ar.Position = offset + size;
                return bulk;
            case WwiseGameFileSource gameFileSource when Ar.SupportPartialReads && gameFileSource.File is { } file:
                var temp = new FDeferredByteData(file, offset, size);
                Ar.Position = offset + size;
                return temp;
            default:
                Ar.Position = offset;
                return new FDeferredByteData(Ar.ReadBytes(size));
        }
    }

    /// Reads only the SoundBankId from a .bnk file
    /// In order to quickly find the SoundBank without parsing the entire file
    public static uint? TryReadSoundBankId(FArchive Ar)
    {
        while (Ar.Position < Ar.Length)
        {
            var sectionIdentifier = Ar.Read<EChunkID>();
            var sectionLength = Ar.Read<int>();
            var sectionStart = Ar.Position;

            if (sectionIdentifier is EChunkID.BankHeader)
            {
                Ar.Read<uint>(); // Version
                return Ar.Read<uint>(); // SoundBankId
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
