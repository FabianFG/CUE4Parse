using CUE4Parse.UE4.Assets.Exports;
using CUE4Parse.UE4.Objects.Engine;
using CUE4Parse.GameTypes.GhostwireTokyo.Assets;

namespace CUE4Parse_Conversion.Dto;

public sealed class WorldDto : SceneDto
{
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

        var worldPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var ptr in world.StreamingLevels ?? [])
        {
            ct.ThrowIfCancellationRequested();
            switch (ptr.Load())
            {
                case ULevelStreaming { WorldAsset: { } worldAsset } streaming when worldAsset.TryLoad<UWorld>(out var w):
                {
                    if (worldPaths.Add(w.GetPathName()))
                        StreamingLevels.Add(new StreamingLevel(w, streaming));
                    break;
                }
            }
        }

        foreach (var child in GhostwireWorldResolver.ResolveRootChildren(world, ct))
        {
            ct.ThrowIfCancellationRequested();
            if (worldPaths.Add(child.GetPathName()))
                StreamingLevels.Add(new StreamingLevel(child, true));
        }
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
