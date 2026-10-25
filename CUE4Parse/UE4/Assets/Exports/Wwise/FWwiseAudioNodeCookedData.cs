using CUE4Parse.UE4.Assets.Objects;
using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Assets.Utils;
using CUE4Parse.UE4.Objects.UObject;
using Newtonsoft.Json;

namespace CUE4Parse.UE4.Assets.Exports.Wwise;

[StructFallback]
public class FWwiseAudioNodeCookedData
{
    public readonly uint AudioNodeId;
    public readonly FWwiseSoundBankCookedData[] SoundBanks;
    public readonly FWwiseMediaCookedData[] Media;
    public readonly FWwiseExternalSourceCookedData[] ExternalSources;
    public readonly EWwiseAudioNodeLoading AudioNodeLoading;
    public readonly EWwiseAssetDestroyOptions DestroyOptions;
    public readonly FName DebugName;

    public FWwiseAudioNodeCookedData(FStructFallback fallback)
    {
        AudioNodeId = (uint)fallback.GetOrDefault<int>(nameof(AudioNodeId), comparisonType: StringComparison.OrdinalIgnoreCase);
        SoundBanks = fallback.GetOrDefault<FWwiseSoundBankCookedData[]>(nameof(SoundBanks), []);
        Media = fallback.GetOrDefault<FWwiseMediaCookedData[]>(nameof(Media), []);
        ExternalSources = fallback.GetOrDefault<FWwiseExternalSourceCookedData[]>(nameof(ExternalSources), []);
        AudioNodeLoading = fallback.GetOrDefault<EWwiseAudioNodeLoading>(nameof(AudioNodeLoading));
        DestroyOptions = fallback.GetOrDefault<EWwiseAssetDestroyOptions>(nameof(DestroyOptions));;
        DebugName = fallback.GetOrDefault<FName>(nameof(DebugName));
    }

    public void SerializeBulkData(FAssetArchive Ar)
    {
        foreach (var sb in SoundBanks)
            sb.SerializeBulkData(Ar);

        foreach (var media in Media)
            media.SerializeBulkData(Ar);
    }
}

[JsonConverter(typeof(EnumConverter<EWwiseAudioNodeLoading>))]
public enum EWwiseAudioNodeLoading : byte
{
    AlwaysLoad,
    LoadOnReference,
    LoadOnResolve,
    LoadOnEnqueue
}

[JsonConverter(typeof(EnumConverter<EWwiseAssetDestroyOptions>))]
public enum EWwiseAssetDestroyOptions : byte
{
    StopEventOnDestroy,
    WaitForEventEnd
}
