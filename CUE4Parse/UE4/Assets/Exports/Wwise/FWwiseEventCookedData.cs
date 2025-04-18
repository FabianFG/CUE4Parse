using System.Collections.Generic;
using CUE4Parse.UE4.Assets.Objects;
using CUE4Parse.UE4.Assets.Utils;
using CUE4Parse.UE4.Objects.UObject;

namespace CUE4Parse.UE4.Assets.Exports.Wwise;

[StructFallback]
public readonly struct FWwiseEventCookedData
{
    public readonly int EventId;
	public readonly FWwiseSoundBankCookedData[] SoundBanks;
	public readonly FWwiseMediaCookedData[] Media;
	public readonly FWwiseExternalSourceCookedData[] ExternalSources;
	public readonly FWwiseSwitchContainerLeafCookedData[] SwitchContainerLeaves;
	public readonly UScriptSet RequiredGroupValueSet; // FWwiseGroupValueCookedData[]
	public readonly EWwiseEventDestroyOptions DestroyOptions;
	public readonly FName DebugName;

    public FWwiseEventCookedData(FStructFallback fallback)
    {
        EventId = fallback.GetOrDefault<int>(nameof(EventId));
        SoundBanks = fallback.GetOrDefault<FWwiseSoundBankCookedData[]>(nameof(SoundBanks), []);
        Media = fallback.GetOrDefault<FWwiseMediaCookedData[]>(nameof(Media), []);
        ExternalSources = fallback.GetOrDefault<FWwiseExternalSourceCookedData[]>(nameof(ExternalSources), []);
        SwitchContainerLeaves = fallback.GetOrDefault<FWwiseSwitchContainerLeafCookedData[]>(nameof(SwitchContainerLeaves), []);
        RequiredGroupValueSet = fallback.GetOrDefault<UScriptSet>(nameof(RequiredGroupValueSet));
        DestroyOptions = fallback.GetOrDefault<EWwiseEventDestroyOptions>(nameof(DestroyOptions));
        DebugName = fallback.GetOrDefault<FName>(nameof(DebugName));
    }
}
