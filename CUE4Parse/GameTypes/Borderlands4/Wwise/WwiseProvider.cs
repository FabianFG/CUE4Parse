using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Resources;
using CUE4Parse.UE4.Objects.UObject;
using CUE4Parse.UE4.Wwise.Objects.HIRC.Containers;
using CUE4Parse.Utils;
using Newtonsoft.Json;
using Serilog;

namespace CUE4Parse.UE4.Wwise;

// Borderlands 4 uses their own system called GbxAudio, which is a custom wrapper around standard Wwise
public partial class WwiseProvider
{
    private readonly record struct SoundTagData(uint Id, string Event);
    private Dictionary<string, SoundTagData> _bl4SoundTagsMap = [];

    public List<WwiseExtractedSound> ExtractAudioEventBorderlands4(FName audioEventName, bool useSoundTag)
    {
        DetermineBaseWwiseAudioPath();
        PopulateSoundTagsMapBorderlands4();

        _visitedHierarchies.Clear();
        _visitedWemIds.Clear();

        if (useSoundTag)
        {
            if (!_bl4SoundTagsMap.TryGetValue(audioEventName.Text, out var soundTagData))
            {
                Log.Warning($"Couldn't find Sound Tag '{audioEventName.Text}' in the Sound Tags map");
                return [];
            }

            audioEventName = new FName(soundTagData.Event);
            // audioEventId = soundTagData.Id; // Should be used but doesn't match for whatever reason, hashing event name gives correct output anyway
        }

        uint audioEventId = WwiseFnv.GetHash(audioEventName.Text.SubstringAfterLast('.'));

        var results = new List<WwiseExtractedSound>();
        foreach (var hierarchy in GetHierarchiesById(audioEventId))
        {
            if (hierarchy.Data is HierarchyEvent hierarchyEvent)
            {
                LoopThroughEventActions(hierarchyEvent, results, _baseWwiseAudioPath, audioEventName.Text.SubstringAfterLast('.'));
            }
        }

        return results;
    }

    // This data comes from .ncs (Nexus Config Store tables), see https://github.com/Cr4nkSt4r/Borderlands-4.NcsParser
    private void PopulateSoundTagsMapBorderlands4()
    {
        if (_bl4SoundTagsMap.Count > 0)
            return;

        using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("CUE4Parse.Resources.BL4SoundTagsMap.json") ?? throw new MissingManifestResourceException("Couldn't find BL4SoundTagsMap.json in Embedded Resources");
        using StreamReader reader = new(stream);

        _bl4SoundTagsMap = JsonConvert.DeserializeObject<Dictionary<string, SoundTagData>>(reader.ReadToEnd()) ?? [];
    }
}
