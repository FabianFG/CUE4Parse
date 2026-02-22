using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using CUE4Parse.FileProvider;
using CUE4Parse.UE4.Assets.Exports;
using CUE4Parse.UE4.Assets.Exports.Fmod;
using CUE4Parse.UE4.FMod.Objects;
using CUE4Parse.UE4.FMod.Utils;
using CUE4Parse.UE4.Objects.Core.Misc;
using CUE4Parse.Utils;
using Fmod5Sharp.FmodTypes;
using Serilog;
using UE4Config.Parsing;

namespace CUE4Parse.UE4.FMod;

public class FModExtractedSound
{
    public required string Name { get; init; }
    public required string Extension { get; init; }
    public required byte[] Data { get; init; }

    public override string ToString() => Name + "." + Extension.ToLowerInvariant();
}

public class FModProvider
{
    private Dictionary<FModGuid, List<FmodSample>> _resolvedEventsCache = [];
    private Dictionary<FModGuid, bool> _eventResolutionStatus = [];
    private Dictionary<FModGuid, FModGuid> _eventToReaderMap = [];
    private Dictionary<FModGuid, FModReader> _mergedReaders = [];
    private static byte[]? _encryptionKey;
    private string? _BankOutputDirectory;

    public FModProvider(IFileProvider provider, string gameDirectory)
    {
        LoadFModSettings(provider);
        LoadPakBanks(provider);
        LoadFileBanks(gameDirectory);
        UpdateEventCache();
    }

    private void LoadPakBanks(IFileProvider provider)
    {
        var banks = provider.Files.Values
            .Where(x => x.Extension == "bank" && x.Path.Contains("FMOD", StringComparison.OrdinalIgnoreCase))
            .GroupBy(x => x.Name.SubstringBefore('.'));

        foreach (var group in banks)
        {
            FModReader? mergedBank = null;
            foreach (var file in group)
            {
                if (!provider.TrySaveAsset(file, out var data)) continue;
                if (!TryLoadBank(new MemoryStream(data), file.Name, out var fmodBank))
                {
                    Log.Error("Failed to serialize FMOD Bank file {bank}", file);
                    continue;
                }

                if (mergedBank == null)
                {
                    mergedBank = fmodBank;
                }
                else
                {
                    mergedBank.Merge(fmodBank);
                }
            }

            if (mergedBank == null) continue;

            var guid = mergedBank.GetBankGuid();
            if (_mergedReaders.TryGetValue(guid, out var existing))
            {
                existing.Merge(mergedBank);
            }
            else
            {
                _mergedReaders[guid] = mergedBank;
            }
        }
    }

    private void LoadFileBanks(string gameDirectory)
    {
        var dir = new DirectoryInfo(gameDirectory);
        if (!dir.Name.Equals("Paks", StringComparison.OrdinalIgnoreCase))
            return;

        if (Directory.GetParent(gameDirectory) is {} parentInfo)
            gameDirectory = parentInfo.FullName;

        string? fmodDir = null!;
        if (!string.IsNullOrEmpty(_BankOutputDirectory))
        {
            var potentialPath = Path.Combine(gameDirectory, _BankOutputDirectory);
            if (Directory.Exists(potentialPath))
                fmodDir = potentialPath;
        }

        fmodDir ??= Directory.EnumerateDirectories(gameDirectory, "FMOD", SearchOption.AllDirectories)
                .SelectMany(fmodFolder => Directory.GetDirectories(fmodFolder, "Desktop", SearchOption.AllDirectories))
                .FirstOrDefault(Directory.Exists);

        if (fmodDir is null)
        {
            Log.Warning("FMOD Desktop directory not found under {0}", gameDirectory);
            return;
        }

        var banks = Directory.GetFiles(fmodDir, "*.bank", SearchOption.AllDirectories)
            .GroupBy(path => Path.GetFileName(path).SubstringBefore('.'));

        foreach (var group in banks)
        {
            FModReader? mergedBank = null;
            foreach (var file in group)
            {
                if (!TryLoadBank(File.OpenRead(file), Path.GetFileNameWithoutExtension(file), out var fmodBank))
                {
                    Log.Error("Failed to serialize FMOD Bank file {bank}", file);
                    continue;
                }

                if (mergedBank == null)
                {
                    mergedBank = fmodBank;
                }
                else
                {
                    mergedBank.Merge(fmodBank);
                }
            }

            if (mergedBank == null) continue;

            var guid = mergedBank.GetBankGuid();
            if (_mergedReaders.TryGetValue(guid, out var existing))
            {
                existing.Merge(mergedBank);
            }
            else
            {
                _mergedReaders[guid] = mergedBank;
            }
        }
    }

    private void LoadFModSettings(IFileProvider provider)
    {
        var engineConfig = provider.DefaultEngine;
        if (engineConfig is null)
            return;

        var values = new List<string>();
        engineConfig.EvaluatePropertyValues("/Script/FMODStudio.FMODSettings", "BankOutputDirectory", values);
        var path = values.FirstOrDefault()?.SubstringAfter("Path=\"").SubstringBefore("\")");
        if (!string.IsNullOrEmpty(path))
        {
            _BankOutputDirectory = path.Replace('\\', Path.DirectorySeparatorChar).Replace('/', Path.DirectorySeparatorChar);
        }

        var fmodSection = engineConfig.Sections
            .FirstOrDefault(s => s.Name == "/Script/FMODStudio.FMODSettings");

        var token = fmodSection?.Tokens
            .OfType<InstructionToken>()
            .FirstOrDefault(t => t.Key == "StudioBankKey");

        if (!string.IsNullOrEmpty(token?.Value))
        {
            _encryptionKey = Encoding.UTF8.GetBytes(Regex.Unescape(token.Value.Trim('"')));
            Log.Information($"FMod encryption key found: {token.Value}");
        }
        else
        {
#if DEBUG
            Log.Debug("FMod encryption key not found in DefaultEngine.ini. Soundbanks might not be encrypted");
#endif
        }
    }

    public bool TryLoadBank(Stream stream, string bankName, [NotNullWhen(true)]out FModReader? fmodReader)
    {
        fmodReader = null;
        try
        {
            using var reader = new BinaryReader(stream);
            fmodReader = new FModReader(reader, bankName, _encryptionKey);
            return true;
        }
        catch (Exception e)
        {
            Log.Error(e, "Can't load FMOD bank");
            return false;
        }
    }

    public void UpdateEventCache()
    {
        var eventSamples = new Dictionary<FModGuid, List<FmodSample>>();

        foreach (var fmodReader in _mergedReaders.Values)
        {
            var resolvedEvents = EventNodesResolver.TryResolveAudioEvents(fmodReader, out bool isFullyResolved);

#if DEBUG
            EventNodesResolver.LogMissingSamples(fmodReader, resolvedEvents);
#endif

            foreach (var kvp in resolvedEvents)
            {
                _eventResolutionStatus.TryAdd(kvp.Key, isFullyResolved);
                _eventToReaderMap[kvp.Key] = fmodReader.GetBankGuid();

                if (kvp.Value.Count is 0)
                    continue;

                if (!eventSamples.TryGetValue(kvp.Key, out var sampleList))
                {
                    eventSamples[kvp.Key] = sampleList = [];
                }

                sampleList.AddRange(kvp.Value);
            }
        }

        _resolvedEventsCache = eventSamples;
    }

    public List<FModExtractedSound> ExtractEventSounds(UFMODEvent audioEvent)
    {
        if (!audioEvent.TryGet<FGuid>("AssetGuid", out var fguid)) return [];
        var eventGuid = new FModGuid(fguid);

        if (!_resolvedEventsCache.TryGetValue(eventGuid, out var samples))
        {
            // There's no way of associating events with samples from sound table, so we just provide all sounds from sound table
            // only if all samples were resolved because if they weren't it might be an issue on our side
            if (_eventResolutionStatus.TryGetValue(eventGuid, out var isResolved) && isResolved)
            {
                Log.Debug("FMODEvent with guid {0} wasn't found in events cache, but all waveforms were resolved, using Sound Table instead", eventGuid);
                return ExtractBankSoundTable(_mergedReaders[_eventToReaderMap[eventGuid]]);
            }

            Log.Warning("Can't find FMODEvent with the guid {0}", eventGuid);
            return [];
        }

        return ExtractAudioSamples(samples, audioEvent.Name);
    }

    public List<FModExtractedSound> ExtractBankSounds(UFMODBank audioBank)
    {
        var assetGuid = audioBank.GetOrDefault<FGuid>("AssetGuid");
        var bankGuid = new FModGuid(assetGuid);

        if (!_mergedReaders.TryGetValue(bankGuid, out var bank))
        {
            Log.Warning("Can't find FMODBank with the guid {0}", bankGuid);
            return [];
        }

        var samples = bank.ExtractTracks();

        return ExtractAudioSamples(samples, audioBank.Name);
    }

    public List<FModExtractedSound> ExtractBankSoundTable(FModReader fmodReader)
        => ExtractAudioSamples(fmodReader.ExtractSoundTableTracks(), fmodReader.BankName);
    public List<FModExtractedSound> ExtractBankSounds(FModReader fmodReader)
       => ExtractAudioSamples(fmodReader.ExtractTracks(), fmodReader.BankName);
    
    private List<FModExtractedSound> ExtractAudioSamples(List<FmodSample> samples, string fallbackSampleName)
    {
        var extracted = new List<FModExtractedSound>(samples.Count);
        for (var i = 0; i < samples.Count; i++)
        {
            var sample = samples[i];
            if (!sample.RebuildAsStandardFileFormat(out var dataBytes, out var fileExtension))
                continue;

            extracted.Add(new FModExtractedSound
            {
                Name = sample.Name ?? $"{fallbackSampleName}_{i}",
                Extension = fileExtension,
                Data = dataBytes
            });
        }

        return extracted;
    }
}
