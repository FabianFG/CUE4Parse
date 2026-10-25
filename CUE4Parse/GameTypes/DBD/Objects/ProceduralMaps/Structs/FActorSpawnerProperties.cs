using CUE4Parse.UE4.Assets.Objects;
using CUE4Parse.UE4.Assets.Utils;
using CUE4Parse.UE4.Objects.UObject;

namespace CUE4Parse.GameTypes.DBD.Objects.ProceduralMaps.Structs;

[StructFallback]
public readonly struct FActorSpawnerProperties
{
    public readonly FSoftObjectPath SceneElement;
    public readonly float Weight;

    public FActorSpawnerProperties(FStructFallback fallback)
    {
        SceneElement = fallback.GetOrDefault<FSoftObjectPath>(nameof(SceneElement));
        Weight = fallback.GetOrDefault(nameof(Weight), 1f);
    }
}
