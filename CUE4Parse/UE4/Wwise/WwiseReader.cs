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
public sealed record WwiseBulkDataSource(FAssetArchive AssetAr, FByteBulkData bulkData) : WwiseDataSource;

[JsonConverter(typeof(WwiseConverter))]
public class WwiseReader
{
    public string Path;
    private readonly WwiseDataSource? _source;

    public AkBankHeader Header { get; }
    public List<WwiseReader>? AKPKBankEntries { get; }
    public List<AkEntry>? AKPKWemEntries { get; }
    public Dictionary<uint, string>? AKPluginList { get; }
    public MediaHeader[]? WemIndexes { get; }
    public Hierarchy[]? Hierarchies { get; }
    public Dictionary<uint, string> BankIDToFileName { get; } = [];
    public string? Platform { get; }
    public Dictionary<string, FDeferredByteData> WwiseEncodedMedias { get; } = [];
    public GlobalSettings? GlobalSettings { get; }
    public CAkEnvironmentsMgr? EnvSettings { get; }
    public FDeferredByteData? WemFile { get; }
    public FDeferredByteData? MidiData { get; }
    public bool IsPlugin { get; }
    public long LoadedSize { get; }
    public long TotalSize { get; }

    public WwiseReader(FWwiseArchive Ar, WwiseDataSource source, long size = -1)
    {
        Path = Ar.Name;
        _source = source;
        TotalSize = size == -1 ? Ar.Length : size;
        var end = size == -1 ? Ar.Length : Ar.Position + size;

        while (Ar.Position < end)
        {
            var sectionIdentifier = Ar.Read<EChunkID>();
            var sectionLength = Ar.Read<int>();
            var position = Ar.Position;

            switch (sectionIdentifier)
            {
                case EChunkID.AKPK:
                    var akpkHeader = new FAKPKHeader(Ar);
                    if (!akpkHeader.Endianness)
                        throw new ParserException(Ar, $"'{Ar.Name}' has unsupported endianness.");

                    Ar.Position = FAKPKHeader.NamesOffset;
                    var folders = Ar.ReadArray(() => new AkFolder(Ar));
                    foreach (var folder in folders)
                        folder.PopulateName(Ar, FAKPKHeader.NamesOffset);

                    Ar.Position = akpkHeader.BanksOffset;
                    var bankEntries = Ar.ReadArray(() => new AkEntry(Ar, true, false));

                    AKPKWemEntries = [];
                    Ar.Position = akpkHeader.WemsOffset;
                    AKPKWemEntries.AddRange(Ar.ReadArray(() => new AkEntry(Ar, false, false)));
                    Ar.Position = akpkHeader.ExternalWemsOffset;
                    AKPKWemEntries.AddRange(Ar.ReadArray(() => new AkEntry(Ar, false, true)));

                    var saved = Ar.Position;
                    AKPKBankEntries = [];
                    foreach (var entry in bankEntries)
                    {
                        entry.ReadAudioPath(folders);
                        Ar.Position = entry.Offset * entry.OffsetMultiplier;
                        var bank = new WwiseReader(Ar, _source, entry.Size) { Path = entry.AudioPath };
                        LoadedSize += bank.LoadedSize;
                        AKPKBankEntries.Add(bank);
                    }

                    foreach (var entry in AKPKWemEntries)
                    {
                        entry.ReadAudioPath(folders);
                        LoadedSize += entry.ReadData(Ar, _source);
                        WwiseEncodedMedias[entry.Name] = entry.Data;
                    }

                    // return cause we got everything else from entries
                    return;
                case EChunkID.BankHeader:
                    LoadedSize += sectionLength;
                    Header = new AkBankHeader(Ar, sectionLength);

                    Ar.Version = Header.Version;

                    if (!Ar.IsSupported())
                        Log.Warning($"Wwise version {Ar.Version} is not supported");
                    break;
                case EChunkID.BankInit:
                    LoadedSize += sectionLength;
                    AKPluginList = Ar.ReadMap(Ar.Read<uint>, () => Ar.Version <= 136 ? Ar.ReadFString() : Ar.ReadStzString());
                    break;
                case EChunkID.BankDataIndex:
                    LoadedSize += sectionLength;
                    WemIndexes = Ar.ReadArray(sectionLength / 12, Ar.Read<MediaHeader>);
                    break;
                case EChunkID.BankData:
                    if (WemIndexes == null)
                        break;

                    for (var i = 0; i < WemIndexes.Length; i++)
                    {
                        var wemData = WemIndexes[i];
                        if (wemData.Id == 0)
                            continue;

                        var temp = ReadDeferredByteData(Ar, _source, position + wemData.Offset, wemData.Size);
                        LoadedSize += temp.LoadedSize;
                        WwiseEncodedMedias[wemData.Id.ToString()] = temp;
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
                    if (Ar.IsSupported()) // Let's guard this just in case
                    {
                        LoadedSize += sectionLength;
                        GlobalSettings = new GlobalSettings(Ar);
                    }
                    break;
                case EChunkID.BankEnvSetting:
                    if (Ar.IsSupported()) // Let's guard this just in case
                    {
                        LoadedSize += sectionLength;
                        EnvSettings = new CAkEnvironmentsMgr(Ar);
                    }
                    break;
                case EChunkID.FXPR:
                    break;
                case EChunkID.BankCustomPlatformName:
                    LoadedSize += sectionLength;
                    Platform = Ar.Version <= 136 ? Encoding.ASCII.GetString(Ar.ReadArray<byte>()).TrimEnd('\0') : Ar.ReadStzString();
                    break;
                case EChunkID.PLUGIN:
                    // Plugin container holds audio data encoded specifically for a given Wwise plugin
                    // For example: ADM3 codec (Crankcase Audio), AK Convolution Reverb impulse response (currently not supported https://github.com/vgmstream/vgmstream/issues/1638)
                    Ar.Position -= 8;
#if DEBUG
                    Log.Debug($"Found Wwise plugin section with length {sectionLength}");
#endif
                    WemFile = ReadDeferredByteData(Ar, _source, Ar.Position, 8 + sectionLength);
                    LoadedSize += WemFile.LoadedSize;
                    IsPlugin = true;
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
    }

    public static FDeferredByteData ReadDeferredByteData(FArchive Ar, WwiseDataSource source, long offset, int size)
    {
        switch (source)
        {
            case WwiseBulkDataSource bulkDataSource when Ar.SupportPartialReads && bulkDataSource.AssetAr is { } assetAr && bulkDataSource.bulkData is { } bulkData:
                var newBulkData = new FBulkDataDeferredByteData(assetAr, bulkData, offset, size);
                Ar.Position = offset + size;
                return newBulkData;
            case WwiseGameFileSource gameFileSource when Ar.SupportPartialReads && gameFileSource.File is { } file:
                var gameFileData = new FGameFileDeferredByteData(file, offset, size);
                Ar.Position = offset + size;
                return gameFileData;
            default:
                Ar.Position = offset;
                return new FArrayDeferredByteData(Ar.ReadBytes(size));
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
}
