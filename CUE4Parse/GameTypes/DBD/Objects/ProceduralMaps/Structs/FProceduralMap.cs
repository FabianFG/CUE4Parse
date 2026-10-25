using CUE4Parse.UE4.Assets.Objects;
using CUE4Parse.UE4.Assets.Utils;
using CUE4Parse.UE4.Objects.UObject;

namespace CUE4Parse.GameTypes.DBD.Objects.ProceduralMaps.Structs;

[StructFallback]
public readonly struct FProceduralMap(FStructFallback fallback)
{
    public readonly FName MapId = fallback.GetOrDefault<FName>(nameof(MapId));
    public readonly float HookMinDistance = fallback.GetOrDefault(nameof(HookMinDistance), -1f);
    public readonly int HookMinCount = fallback.GetOrDefault(nameof(HookMinCount), -1);
    public readonly int HookMaxCount = fallback.GetOrDefault(nameof(HookMaxCount), -1);
    public readonly float BookShelvesMinDistance = fallback.GetOrDefault(nameof(BookShelvesMinDistance), -1f);
    public readonly int BookShelvesMinCount = fallback.GetOrDefault(nameof(BookShelvesMinCount), -1);
    public readonly int BookShelvesMaxCount = fallback.GetOrDefault(nameof(BookShelvesMaxCount), -1);
}
