using CUE4Parse.UE4.Assets.Objects;
using CUE4Parse.UE4.Assets.Utils;
using CUE4Parse.UE4.Objects.UObject;

namespace CUE4Parse.UE4.Assets.Exports.Wwise;

[StructFallback]
public readonly struct FWwiseLanguageCookedData
{
    public readonly int LanguageId;
    public readonly FName LanguageName;
    public readonly EWwiseLanguageRequirement LanguageRequirement;

    public FWwiseLanguageCookedData(FStructFallback fallback)
    {
        LanguageId = fallback.GetOrDefault<int>(nameof(LanguageId));
        LanguageName = fallback.GetOrDefault<FName>(nameof(LanguageName));
        LanguageRequirement = fallback.GetOrDefault<EWwiseLanguageRequirement>(nameof(LanguageRequirement));
    }
}
