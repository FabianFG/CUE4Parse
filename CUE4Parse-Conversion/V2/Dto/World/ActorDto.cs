using System;
using System.Collections.Generic;
using CUE4Parse.UE4.Assets.Exports;
using CUE4Parse.UE4.Assets.Exports.Actor;
using CUE4Parse.UE4.Objects.UObject;

namespace CUE4Parse_Conversion.V2.Dto.World;

public class ActorDto : ObjectDto
{
    public readonly SceneComponentDto? RootComponent;
    public readonly List<ActorDto> ChildActors = [];
    public readonly bool IsVisible = true;

    internal ActorDto(UObject actor, WorldParseContext ctx) : base(actor, actor is AActor a && !string.IsNullOrWhiteSpace(a.ActorLabel) ? a.ActorLabel : null)
    {
        foreach (var component in FindComponents(actor))
        {
            var c = ctx.GetOrCreate(component, this);
            if (RootComponent == null && c is SceneComponentDto root)
            {
                RootComponent = root;
            }
        }

        if (actor.TryGetValue(out bool hidden, "bHidden"))
        {
            IsVisible = !hidden;
        }
    }

    internal void AddChildActor(ActorDto child) => ChildActors.Add(child);

    private IEnumerable<FPackageIndex?> FindComponents(UObject actor)
    {
        yield return actor.GetOrDefault<FPackageIndex?>("RootComponent");
        yield return actor.GetOrDefault<FPackageIndex?>("SplineComponent");

        foreach (var ptr in actor.GetOrDefault<FPackageIndex?[]>("InstanceComponents", []))
            yield return ptr;
        foreach (var ptr in actor.GetOrDefault<FPackageIndex?[]>("BlueprintCreatedComponents", []))
            yield return ptr;

        foreach (var ptr in actor.GetOrDefault<FPackageIndex?[]>("LandscapeComponents", []))
            yield return ptr;

        if (actor is AInstancedFoliageActor { FoliageInfos: { } foliages })
        {
            foreach (var foliage in foliages.Values)
            {
                switch (foliage.Implementation)
                {
                    case FFoliageStaticMesh staticMesh:
                        yield return staticMesh.Component;
                        break;
                    case FFoliageActor:
                        throw new NotImplementedException("FoliageActor is not supported yet");
                }
            }
        }
    }

    public override string ToString() => $"{base.ToString()} (RootComponent: {RootComponent?.Name ?? "none"}, ChildActors: {ChildActors.Count}, Visible: {IsVisible})";

    public override void Dispose()
    {
        ChildActors.Clear();
    }
}
