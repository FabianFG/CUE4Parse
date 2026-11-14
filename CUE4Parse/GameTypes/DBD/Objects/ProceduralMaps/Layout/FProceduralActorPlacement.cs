using CUE4Parse.UE4.Objects.Core.Math;
using CUE4Parse.UE4.Objects.Engine;
using CUE4Parse.GameTypes.DBD.Objects.ProceduralMaps.Structs;

namespace CUE4Parse.GameTypes.DBD.Objects.ProceduralMaps.Layout;

public sealed record FProceduralActorPlacement(string Name, UBlueprintGeneratedClass Blueprint, FTransform Transform)
{
    public IReadOnlyDictionary<string, bool>? SpawnerActivation { get; internal set; }
    public IReadOnlyDictionary<string, UBlueprintGeneratedClass>? ComponentVariations { get; internal set; }
    public IReadOnlyDictionary<string, FActorSpawnerProperties[]>? SpawnerElements { get; internal set; }
}
