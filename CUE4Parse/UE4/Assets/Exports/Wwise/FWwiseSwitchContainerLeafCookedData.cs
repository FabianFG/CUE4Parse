using System.Collections.Generic;
using CUE4Parse.UE4.Assets.Objects;
using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Assets.Utils;

namespace CUE4Parse.UE4.Assets.Exports.Wwise;

[StructFallback]
public readonly struct FWwiseSwitchContainerLeafCookedData
{
    public readonly UScriptSet GroupValueSet; // FWwiseGroupValueCookedData[]
    public readonly FWwiseSoundBankCookedData[] SoundBanks;
    public readonly FWwiseMediaCookedData[] Media;
    public readonly FWwiseExternalSourceCookedData[] ExternalSources;
    public readonly FWwisePackagedFile? PackagedFile;

    public FWwiseSwitchContainerLeafCookedData(FStructFallback fallback)
    {
        GroupValueSet = fallback.GetOrDefault<UScriptSet>(nameof(GroupValueSet));
        SoundBanks = fallback.GetOrDefault<FWwiseSoundBankCookedData[]>(nameof(SoundBanks), []);
        Media = fallback.GetOrDefault<FWwiseMediaCookedData[]>(nameof(Media), []);
        ExternalSources = fallback.GetOrDefault<FWwiseExternalSourceCookedData[]>(nameof(ExternalSources), []);
        PackagedFile = FWwisePackagedFile.CreatePackagedFile(fallback, nameof(PackagedFile));
    }

    public void SerializeBulkData(FAssetArchive Ar)
    {
        PackagedFile?.SerializeBulkData(Ar);
    }
}
