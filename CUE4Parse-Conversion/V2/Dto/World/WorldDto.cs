using System;
using System.Collections.Generic;
using CUE4Parse.UE4.Assets.Exports;
using CUE4Parse.UE4.Objects.Engine;

namespace CUE4Parse_Conversion.V2.Dto.World;

public class WorldDto : ObjectDto
{
    public readonly List<ActorDto> Actors = [];
    public readonly List<UWorld> StreamingLevels = [];

    public WorldDto(UWorld world) : this(world, new WorldParseContext())
    {

    }

    private WorldDto(UWorld world, WorldParseContext ctx) : base(world)
    {
        var level = world.PersistentLevel.Load<ULevel>();
        ArgumentNullException.ThrowIfNull(level, "Failed to load persistent level");

        foreach (var ptr in level.Actors)
        {
            if (ptr == null || !ptr.TryLoad<UObject>(out var data))
                continue;

            var actor = new ActorDto(data, ctx);
            if (actor.RootComponent?._attachParent == null)
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
        foreach (var actor in Actors)
        {
            actor.Dispose();
        }
        Actors.Clear();
        StreamingLevels.Clear();
    }
}
