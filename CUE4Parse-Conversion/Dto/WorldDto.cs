using CUE4Parse.UE4.Assets.Exports;
using CUE4Parse.UE4.Objects.Engine;

namespace CUE4Parse_Conversion.Dto;

public class WorldDto : ObjectDto
{
    public readonly List<ActorDto> Actors = [];
    public readonly List<StreamingLevel> StreamingLevels = [];

    public bool IsEmpty => Actors.Count == 0 && StreamingLevels.Count == 0;

    public WorldDto(UWorld world, CancellationToken ct = default) : this(world, new WorldParseContext(), ct)
    {

    }

    private WorldDto(UWorld world, WorldParseContext ctx, CancellationToken ct = default) : base(world)
    {
        var level = world.PersistentLevel.Load<ULevel>();
        ArgumentNullException.ThrowIfNull(level, "Failed to load persistent level");

        foreach (var ptr in level.Actors)
        {
            ct.ThrowIfCancellationRequested();
            if (ptr == null || !ptr.TryLoad<UObject>(out var data))
                continue;

            var actor = ActorDto.Create(data, ctx);
            if (actor.RootComponent?._attachParent == null)
            {
                Actors.Add(actor);
            }
        }

        foreach (var ptr in world.StreamingLevels)
        {
            ct.ThrowIfCancellationRequested();
            switch (ptr.Load())
            {
                case ULevelStreaming { WorldAsset: { } worldAsset } streaming when worldAsset.TryLoad<UWorld>(out var w):
                {
                    StreamingLevels.Add(new StreamingLevel(w, streaming));
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

public class StreamingLevel(UWorld world, bool isPersistent)
{
    public readonly UWorld World = world;
    public bool IsPersistent = isPersistent; // non-persistent levels will be referenced but not automatically exported

    public StreamingLevel(UWorld world, ULevelStreaming streaming) : this(world, streaming is ULevelStreamingAlwaysLoaded or ULevelStreamingPersistent)
    {

    }
}
