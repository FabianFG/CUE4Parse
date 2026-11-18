using CUE4Parse.UE4.Assets.Objects;
using CUE4Parse.UE4.Assets.Utils;
using CUE4Parse.UE4.Objects.Core.Math;

namespace CUE4Parse.GameTypes.GTA.Assets.Objects;

[StructFallback]
public readonly struct FWindowSectionInfo
{
    public readonly int SectionIndex;
    public readonly FVector4? SectionSphere;

    public FWindowSectionInfo(FStructFallback fallback)
    {
        SectionIndex = fallback.GetOrDefault<int>(nameof(SectionIndex));
        SectionSphere = fallback.GetOrDefault<FVector4?>(nameof(SectionSphere));
    }
}
