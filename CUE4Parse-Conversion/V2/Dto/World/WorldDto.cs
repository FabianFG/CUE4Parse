using System;
using System.Collections.Generic;
using CUE4Parse.UE4.Assets.Exports;
using CUE4Parse.UE4.Objects.Engine;

namespace CUE4Parse_Conversion.V2.Dto.World;

public class WorldDto : ObjectDto
{
    public readonly HashSet<ActorDto> Actors = [];
    public readonly List<UWorld> StreamingLevels = [];

    public WorldDto(UWorld world) : this(world, new WorldParseContext())
    {

    }

    private WorldDto(UWorld world, WorldParseContext ctx) : base(world)
    {
        var level = world.PersistentLevel.Load<ULevel>();
        ArgumentNullException.ThrowIfNull(level, "Failed to load persistent level");

        // collect all actors + flatten all their components in the ctx
        var actors = new List<ActorDto>();
        foreach (var ptr in level.Actors)
        {
            if (ptr == null || !ptr.TryLoad<UObject>(out var data))
                continue;

            actors.Add(new ActorDto(data, ctx));
        }

        // resolve actor to actor and component to component hierarchy
        ctx.WireHierarchy();

        foreach (var actor in actors)
        {
            // add top-level or orphaned actors to the world
            if (actor.RootComponent == null || actor.RootComponent.Parent == null)
            {
                Actors.Add(actor);
            }
        }

        foreach (var ptr in world.StreamingLevels)
        {
            switch (ptr.Load())
            {
                case ULevelStreaming { WorldAsset: { } worldAsset } streaming when worldAsset.TryLoad<UWorld>(out var w):
                {
                    if (streaming is ULevelStreamingAlwaysLoaded or ULevelStreamingPersistent)
                    {
                        StreamingLevels.Add(w);
                    }
                    else
                    {
                        // TODO: on-demand streaming levels
                    }
                    break;
                }
            }
        }
    }

    public override string ToString() => $"{base.ToString()} (Actors: {Actors.Count}, StreamingLevels: {StreamingLevels.Count})";

    public override void Dispose()
    {
        Actors.Clear();
        StreamingLevels.Clear();
    }
}
