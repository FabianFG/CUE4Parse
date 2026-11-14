using CUE4Parse.UE4.Assets.Objects;
using CUE4Parse.UE4.Assets.Utils;

namespace CUE4Parse.GameTypes.DBD.Objects.ProceduralMaps.Structs;

[StructFallback]
public readonly struct FSpawnerElement(FStructFallback fallback)
{
    public readonly FActorSpawnerProperties[] ObjectsToBeSpawned = fallback.GetOrDefault<FActorSpawnerProperties[]>(nameof(ObjectsToBeSpawned), []);
}
