using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using CUE4Parse.FileProvider.Objects;
using CUE4Parse.FileProvider.Vfs;
using CUE4Parse.UE4.Assets.Exports.Wwise;
using CUE4Parse.UE4.Readers;
using CUE4Parse.UE4.Wwise.Objects;
using CUE4Parse.UE4.Wwise.Objects.HIRC;
using CUE4Parse.Utils;
using Serilog;

namespace CUE4Parse.UE4.Wwise;

public class WwiseExtractedSound
{
    public required string OutputPath { get; set; }
    public required string Extension { get; set; }
    public required byte[] Data { get; set; }
}

public class WwiseProvider
{
    private readonly AbstractVfsFileProvider _provider;
    private readonly string _baseWwiseAudioPath = string.Empty;
    private readonly Dictionary<uint, Hierarchy> _wwiseHierarchyTables = [];
    private readonly Dictionary<string, byte[]> _wwiseEncodedMedia = [];
    private readonly List<string> _wwiseLoadedSoundBanks = [];
    private bool _completedWwiseFullBnkInit = false;

    public WwiseProvider(AbstractVfsFileProvider provider, UAkAudioEvent audioEvent)
    {
        _provider = provider;
        _baseWwiseAudioPath = DetermineBaseWwiseAudioPath(audioEvent);
        BulkInitializeWwiseSoundBanks();
    }

    public List<WwiseExtractedSound> ExtractAudioEventSounds(UAkAudioEvent audioEvent, string audioDirectory)
    {
        var results = new List<WwiseExtractedSound>();
        var visitedWemIds = new HashSet<uint>(); // To prevent duplicates

        var wwiseData = audioEvent.EventCookedData;
        if (wwiseData == null)
            return results;

        foreach (var kvp in wwiseData.Value.EventLanguageMap)
        {
            if (!kvp.Value.HasValue)
                continue;

            var debugName = kvp.Value.Value.DebugName.ToString();
            var audioEventPath = audioEvent.GetPathName().StartsWith("/Game")
                ? string.Concat(_provider.ProjectName, audioEvent.GetPathName().AsSpan(5))
                : audioEvent.GetPathName();

            foreach (var soundBank in kvp.Value.Value.SoundBanks)
            {
                if (!soundBank.bContainsMedia)
                    continue;

                var soundBankName = soundBank.SoundBankPathName.ToString();
                var soundBankPath = Path.Combine(_baseWwiseAudioPath, soundBankName);
                var audioEventId = kvp.Value.Value.EventId.ToString();

                TryLoadAndCacheSoundBank(soundBankPath, soundBankName, out _);

                var visitedDecisionNodes = new HashSet<(uint parentHierarchyId, uint audioNodeId)>(); // To prevent infinite loops (shouldn't happen, just in case)

                if (!long.TryParse(audioEventId, out long parsedId))
                    continue;

                uint parsedAudioEventId = (uint) parsedId;
                if (_wwiseHierarchyTables.TryGetValue(parsedAudioEventId, out var eventHierarchy) &&
                    eventHierarchy.Data is HierarchyEvent hierarchyEvent)
                {
                    foreach (var actionId in hierarchyEvent.EventActionIds)
                    {
                        if (!_wwiseHierarchyTables.TryGetValue(actionId, out var actionHierarchy) ||
                            actionHierarchy.Data is not HierarchyEventAction eventAction)
                            continue;

                        // TODO: If EventActionPlay points to different soundbank ID than we're currently in, use `wwiseReader.IdToString` to convert to bank name, serialize it, and continue traversing from there
                        // This isn't needed if all soundbanks are loaded anyway

                        //if (eventAction.EventActionType == EEventActionType.Play)
                        //{
                        //    var playActionData = (AkActionPlay) eventAction.ActionData;
                        //    var bankId = playActionData.BankId;
                        //    if (bankId != referencedSoundBankId) // I need to know what soundbank I'm currently in
                        //    {
                        //        var soundbankConvertedName = IdToString[referencedSoundBankId]; // I need IdToString from given soundbank
                        //        TryLoadAndCacheSoundBank(Path.Combine(baseWwiseAudioPath, soundbankConvertedName + ".bnk"), soundbankConvertedName, out _);
                        //    }
                        //}

                        TraverseAndSave(eventAction.ReferencedId);
                    }
                }

                void TraverseAndSave(uint id)
                {
                    if (!_wwiseHierarchyTables.TryGetValue(id, out var hierarchy))
                        return;

                    switch (hierarchy.Data)
                    {
                        case HierarchySoundSfxVoice soundSfx:
                            SaveWemSound(soundSfx.Source.SourceId);
                            break;
                        case HierarchyMusicRandomSequenceContainer musicRandomSequenceContainer:
                            foreach (var childId in musicRandomSequenceContainer.ChildIds)
                                TraverseAndSave(childId);
                            break;
                        case HierarchyMusicSwitchContainer musicSwitchContainer:
                            foreach (var childId in musicSwitchContainer.ChildIds)
                                TraverseAndSave(childId);
                            foreach (var node in musicSwitchContainer.DecisionTree.Nodes)
                                foreach (var nodeChild in node.Children)
                                    TraverseDecisionTreeNode(nodeChild, musicSwitchContainer.Id);

                            void TraverseDecisionTreeNode(AkDecisionTreeNode node, uint parentHierarchyId)
                            {
                                var key = (parentHierarchyId, node.AudioNodeId);
                                if (!visitedDecisionNodes.Add(key))
                                    return;

                                foreach (var nodeChildTraverse in node.Children)
                                {
                                    TraverseAndSave(nodeChildTraverse.AudioNodeId);
                                    TraverseDecisionTreeNode(nodeChildTraverse, parentHierarchyId);
                                }
                            }
                            break;
                        case HierarchyMusicTrack musicTrack:
                            foreach (var playlist in musicTrack.Playlist)
                                SaveWemSound(playlist.SourceId);
                            break;
                        case HierarchyMusicSegment musicSegment:
                            foreach (var childId in musicSegment.ChildIds)
                                TraverseAndSave(childId);
                            break;
                        case HierarchyRandomSequenceContainer randomContainer:
                            foreach (var childId in randomContainer.ChildIds)
                                TraverseAndSave(childId);
                            break;
                        case HierarchySwitchContainer switchContainer:
                            foreach (var childId in switchContainer.ChildIds)
                                TraverseAndSave(childId);
                            break;
                        case HierarchyLayerContainer layerContainer:
                            foreach (var childId in layerContainer.ChildIds)
                                TraverseAndSave(childId);
                            break;
                    }
                }

                void SaveWemSound(uint wemId)
                {
                    if (!visitedWemIds.Add(wemId))
                        return;

                    if (_wwiseEncodedMedia.TryGetValue(wemId.ToString(), out var wemData))
                    {
                        var fileName = $"{debugName.Replace('\\', '/')} ({wemId})";
                        var outputPath = Path.Combine(audioEventPath.Replace($".{debugName}", ""), fileName);

                        // If file path is too long, audio player will fail
                        var fullOutputPath = Path.Combine(audioDirectory, outputPath);
                        if (outputPath.StartsWith('/'))
                            outputPath = outputPath[1..];
                        if (fullOutputPath.Length >= 250)
                        {
                            outputPath = Path.Combine(_provider.ProjectName, fileName);
                        }

                        results.Add(new WwiseExtractedSound
                        {
                            OutputPath = outputPath,
                            Extension = "WEM",
                            Data = wemData,
                        });
                    }
                }
            }

            foreach (var media in kvp.Value.Value.Media)
            {
                var mediaRelativePath = Path.Combine(_baseWwiseAudioPath, media.MediaPathName.Text.Replace('\\', '/'));

                if (!_provider.TrySaveAsset(mediaRelativePath, out byte[]? data))
                    continue;

                var mediaDebugName = !string.IsNullOrEmpty(media.DebugName.Text)
                    ? media.DebugName.Text.SubstringBeforeLast('.')
                    : Path.GetFileNameWithoutExtension(mediaRelativePath);

                var namedPath = Path.Combine(
                    _baseWwiseAudioPath,
                    $"{mediaDebugName.Replace('\\', '/')} ({kvp.Key.LanguageName.Text})"
                );

                results.Add(new WwiseExtractedSound
                {
                    OutputPath = namedPath,
                    Extension = Path.GetExtension(mediaRelativePath).TrimStart('.'),
                    Data = data,
                });
            }
        }
        return results;
    }

    private string DetermineBaseWwiseAudioPath(UAkAudioEvent audioEvent)
    {
        var files = _provider.Files.Values.ToList();

        var baseWwiseAudioPath = Path.Combine(_provider.ProjectName, "Content", "WwiseAudio"); // Most common directory

        var wwiseData = audioEvent?.EventCookedData;
        if (wwiseData == null)
            return baseWwiseAudioPath;

        var eventData = wwiseData.Value.EventLanguageMap
            .Select(kv => kv.Value)
            .FirstOrDefault(v => v.HasValue);

        var soundBankName = eventData?.SoundBanks.FirstOrDefault().SoundBankPathName.ToString() ?? string.Empty;
        var mediaPathName = eventData?.Media.FirstOrDefault().MediaPathName.Text ?? string.Empty;

        if (!string.IsNullOrEmpty(soundBankName))
        {
            GameFile? matchingFile = files.FirstOrDefault(f => f.Path.Contains(soundBankName));
            if (matchingFile != null)
            {
                var matchingDirectory = matchingFile.Path[..matchingFile.Path.LastIndexOf(soundBankName)];
                baseWwiseAudioPath = matchingDirectory.Replace('/', Path.DirectorySeparatorChar);
                return baseWwiseAudioPath;
            }
        }

        if (!string.IsNullOrEmpty(mediaPathName))
        {
            GameFile? matchingFile = files.FirstOrDefault(f => f.Path.Contains(mediaPathName));
            if (matchingFile != null)
            {
                var matchingDirectory = matchingFile.Path[..matchingFile.Path.LastIndexOf(mediaPathName)];
                baseWwiseAudioPath = matchingDirectory.Replace('/', Path.DirectorySeparatorChar);
                return baseWwiseAudioPath;
            }
        }

        return baseWwiseAudioPath;
    }

    private void BulkInitializeWwiseSoundBanks()
    {
        if (_completedWwiseFullBnkInit)
            return;

        // Important note: If game splits audio event hierarchies across multiple soundbanks and either of these limits is reached, given game requires custom loading implementation!
        const long MAX_TOTAL_WWISE_SIZE = 2L * 1024 * 1024 * 1024; // 2 GB
        const int MAX_BANK_FILES = 500;

        long totalLoadedSize = 0;
        int totalLoadedBanks = 0;

        IEnumerable<GameFile> soundBankFiles = _provider.Files.Values
            .Where(file => string.Equals(file.Extension, "bnk", StringComparison.OrdinalIgnoreCase))
            .Where(file => file.Path.StartsWith(_baseWwiseAudioPath.Replace("\\", "/"), StringComparison.OrdinalIgnoreCase));

        foreach (var soundbank in soundBankFiles)
        {
            if (totalLoadedBanks >= MAX_BANK_FILES)
            {
#if DEBUG
                Log.Debug("Reached maximum number of soundbank files to load. This game might require custom loading implementation (only necessary if audio event hierarchies are split across multiple soundbanks).");
#endif
                break;
            }

            string fullPath = soundbank.Path;
            string relPath = fullPath[_baseWwiseAudioPath.Length..].TrimStart('/', '\\');

            if (!TryLoadAndCacheSoundBank(fullPath, relPath, out var size))
                continue;

            if (totalLoadedSize + size > MAX_TOTAL_WWISE_SIZE)
            {
#if DEBUG
                Log.Debug("Reached maximum total size of soundbank files to load. This game might require custom loading implementation (only necessary if audio event hierarchies are split across multiple soundbanks).");
#endif
                break;
            }

            totalLoadedSize += size;
            totalLoadedBanks += 1;
        }

        _completedWwiseFullBnkInit = true;
    }

    private bool TryLoadAndCacheSoundBank(string fullAbsolutePath, string relativePath, out long fileSize)
    {
        fileSize = 0;

        if (_wwiseLoadedSoundBanks.Contains(relativePath))
            return false;

        if (!_provider.TrySaveAsset(fullAbsolutePath, out byte[]? data))
            return false;

        fileSize = data.LongLength;

        using var archive = new FByteArchive(relativePath, data);
        var wwiseReader = new WwiseReader(archive);

        if (wwiseReader.Hierarchies != null)
        {
            foreach (var h in wwiseReader.Hierarchies)
            {
                uint id = h.Data.Id;
                if (!_wwiseHierarchyTables.ContainsKey(id))
                    _wwiseHierarchyTables[id] = h;
            }
        }

        if (wwiseReader.WwiseEncodedMedias != null)
        {
            foreach (var kv in wwiseReader.WwiseEncodedMedias)
            {
                if (!_wwiseEncodedMedia.ContainsKey(kv.Key))
                    _wwiseEncodedMedia[kv.Key] = kv.Value;
            }
        }

        _wwiseLoadedSoundBanks.Add(relativePath);
        return true;
    }
}
