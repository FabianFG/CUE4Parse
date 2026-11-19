using CUE4Parse.UE4.Assets.Objects;
using CUE4Parse.UE4.Assets.Utils;

namespace CUE4Parse.GameTypes.GTA.Assets.Objects;

[StructFallback]
public readonly struct FWindowSectionInfoContainer
{
    public readonly FWindowSectionInfo[]? Sections;

    public FWindowSectionInfoContainer(FStructFallback fallback)
    {
        Sections = fallback.GetOrDefault<FWindowSectionInfo[]?>(nameof(Sections));
    }
}
