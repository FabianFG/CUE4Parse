using System.Collections;
using CUE4Parse.UE4.Assets.Exports;
using CUE4Parse.UE4.Assets.Exports.Flash;
using CUE4Parse.UE4.Assets.Exports.Houdini;
using CUE4Parse.UE4.Assets.Exports.Wwise;

namespace CUE4Parse_Conversion.Dto;

public sealed record EmbeddedFileDto(string Name, string Extension, Func<byte[]> ReadData);

/// <summary>Proprietary files embedded in a UE payload.</summary>
public sealed class EmbeddedFilesDto
{
    public IReadOnlyList<EmbeddedFileDto> Files { get; }

    public static bool Supports(UObject export) => export is UHoudiniAsset or USwfMovie
        or UWwiseAssetLibrary or UAkAudioType or UAkAssetData or UAkMediaAssetData;

    public EmbeddedFilesDto(UObject export)
    {
        var files = new List<EmbeddedFileDto>();
        var visited = new HashSet<FWwisePackagedFile>(ReferenceEqualityComparer.Instance);
        switch (export)
        {
            case UHoudiniAsset { HdaBuffer.Length: > 0 } asset:
                files.Add(new(export.Name, "hda", () => asset.HdaBuffer));
                break;
            case USwfMovie { RawData.Length: > 0 } movie:
                files.Add(new(export.Name, "swf", () => movie.RawData));
                break;
            case UWwiseAssetLibrary or UAkAudioType or UAkAssetData or UAkMediaAssetData:
                TraverseWwiseFiles(export);
                break;
        }

        Files = files;

        void TraverseWwiseFiles(object? data, string prefix = "", string? fallbackName = null)
        {
            switch (data)
            {
                case null:
                    return;
                case IDictionary map:
                    foreach (DictionaryEntry entry in map)
                    {
                        var languagePrefix = entry.Key is FWwiseLanguageCookedData language ? (language.LanguageId == 0 ? "" : language.LanguageId + "_") : prefix;
                        TraverseWwiseFiles(entry.Value, languagePrefix, fallbackName);
                    }
                    return;
                case IEnumerable collection:
                    foreach (var child in collection)
                    {
                        TraverseWwiseFiles(child, prefix, fallbackName);
                    }
                    return;
                case FWwisePackagedFile { BulkData: { CanGetBuffer: true, TotalSize: > 0, FileExtension: { } extension } reader } file:
                    if (!visited.Add(file))
                        return;

                    var name = file.PathName.IsNone ? "" : Path.ChangeExtension(file.PathName.Text.Replace('\\', '/'), null);
                    if (string.IsNullOrWhiteSpace(name))
                        name = fallbackName ?? file.Hash.ToString();

                    files.Add(new(prefix + name, extension, reader.GetBuffer));
                    return;
                case UAkAssetData { Data: { CanGetBuffer: true, TotalSize: > 0, FileExtension: { } extension } reader } asset:
                    files.Add(new(asset.Name, extension, reader.GetBuffer));
                    return;
                case UAkMediaAssetData media:
                    // Prefetch is just partial data, we don't want that, there should be data in full somewhere else
                    var chunks = media.DataChunks.Where(chunk => !chunk.IsPrefetch && chunk.Data.GetDataSize() > 0).ToArray();
                    if (chunks.Length > 0)
                    {
                        files.Add(new(media.Name, "wem", () => [.. chunks.SelectMany(chunk => chunk.Data.ReadDataOnce() ?? [])]));
                    }
                    return;
            }

            fallbackName = data switch
            {
                FWwiseSoundBankCookedData bank when !bank.DebugName.IsNone && !string.IsNullOrWhiteSpace(bank.DebugName.Text) => Path.ChangeExtension(bank.DebugName.Text.Replace('\\', '/'), null),
                FWwiseSoundBankCookedData bank => bank.SoundBankId.ToString(),
                FWwiseMediaCookedData media when !media.DebugName.IsNone && !string.IsNullOrWhiteSpace(media.DebugName.Text) => Path.ChangeExtension(media.DebugName.Text.Replace('\\', '/'), null),
                FWwiseMediaCookedData media => media.MediaId.ToString(),
                _ => fallbackName
            };

            // Collect files from nested Wwise data, order matters
            object?[] children = data switch
            {
                UWwiseAssetLibrary asset => [asset.CookedData?.PackagedFiles],
                UAkAudioEvent asset => [asset.EventCookedData],
                UAkInitBank asset => [asset.InitBankCookedData],
                UAkAudioBank asset => [asset.SoundBankCookedData],
                UAkAudioNode asset => [asset.AudioNodeCookedData],
                UAkAuxBus asset => [asset.AuxBusCookedData],
                UAkEffectShareSet asset => [asset.ShareSetCookedData],
                FWwiseLocalizedSoundBankCookedData localized => [localized.SoundBankLanguageMap],
                FWwiseInitBankCookedData bank => [bank.PackagedFile, bank.SoundBanks, bank.Media],
                FWwiseSoundBankCookedData bank => [bank.PackagedFile],
                FWwiseAudioNodeCookedData node => [node.SoundBanks, node.Media],

                FWwiseLocalizedEventCookedData localized => [localized.EventLanguageMap],
                FWwiseLocalizedAuxBusCookedData localized => [localized.AuxBusLanguageMap],
                FWwiseLocalizedShareSetCookedData localized => [localized.ShareSetLanguageMap],
                FWwiseEventCookedData ev => [ev.SoundBanks, ev.Media, ev.AudioNodes, ev.SwitchContainerLeaves],
                FWwiseMediaCookedData media => [media.PackagedFile],
                FWwiseAuxBusCookedData bus => [bus.SoundBanks, bus.Media],
                FWwiseShareSetCookedData shareSet => [shareSet.SoundBanks, shareSet.Media],
                FWwiseSwitchContainerLeafCookedData leaf => [leaf.SoundBanks, leaf.Media, leaf.PackagedFile],
                _ => []
            };
            foreach (var child in children)
            {
                TraverseWwiseFiles(child, prefix, fallbackName);
            }
        }
    }
}
