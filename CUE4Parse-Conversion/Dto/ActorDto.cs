using CUE4Parse.UE4.Assets.Exports;
using CUE4Parse.UE4.Assets.Exports.Actor;
using CUE4Parse.UE4.Objects.Core.Math;
using CUE4Parse.UE4.Objects.Engine;
using CUE4Parse.UE4.Objects.UObject;

namespace CUE4Parse_Conversion.Dto;

public class ActorDto : ObjectDto
{
    public SceneComponentDto? RootComponent { get; protected init; }
    public List<StreamingLevel>? StreamingLevels { get; protected init; }
    public FVector? Location { get; protected init; }
    public FRotator? Rotation { get; protected init; }
    public FVector? Scale { get; protected init; }
    public bool IsVisible { get; } = true;

    protected ActorDto(UObject actor) : base(actor, actor is AActor a && !string.IsNullOrWhiteSpace(a.ActorLabel) ? a.ActorLabel : null)
    {

    }

    private ActorDto(UObject actor, WorldParseContext ctx) : this(actor)
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

        var DrawScale = actor.GetOrDefault("DrawScale", 1.0f);

        var location = actor.GetOrDefault(
            "Location",
            actor.GetOrDefault(
                "RelativeLocation",
                actor.GetOrDefault("Translation", FVector.ZeroVector)));

        var prePivot = actor.GetOrDefault("PrePivot", FVector.ZeroVector);

        Location = location - prePivot;

        if (actor.TryGetValue(out FRotator rotation, "Rotation"))
        {
            Rotation = rotation;
        }

        Scale = actor.GetOrDefault("DrawScale3D", FVector.OneVector * DrawScale);

        if (RootComponent is { } roota && Location is { } loc)
        {
            roota.Transform.Translation = loc;
            if (Rotation is { } rot) roota.Transform.Rotation = rot.Quaternion();
            if (Scale is { } scale) roota.Transform.Scale3D = scale;
        }

        // TODO: TextureData

        if (actor.TryGetValue(out FSoftObjectPath[] additionalWorlds, "AdditionalWorlds"))
        {
            StreamingLevels = [];
            foreach (var additionalWorld in additionalWorlds)
            {
                if (!additionalWorld.TryLoad<UWorld>(out var w)) continue;
                StreamingLevels.Add(new StreamingLevel(w, true));
            }
        }
    }

    internal static ActorDto Create(UObject actor, WorldParseContext ctx) => actor switch
    {
        AWorldSettings ws => new WorldSettingsDto(ws),
        _ => new ActorDto(actor, ctx)
    };

    public bool HasStreamingLevels()
    {
        if (StreamingLevels is { Count: > 0 }) return true;
        return RootComponent?.HasStreamingLevels() ?? false;
    }

    private IEnumerable<FPackageIndex?> FindComponents(UObject actor)
    {
        yield return actor.GetOrDefault<FPackageIndex?>("RootComponent");
        yield return actor.GetOrDefault<FPackageIndex?>("SplineComponent");
        yield return actor.GetOrDefault<FPackageIndex?>("StaticMeshComponent");
        yield return actor.GetOrDefault<FPackageIndex?>("CollisionComponent");

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

    public override string ToString() => $"{base.ToString()} (RootComponent: {RootComponent?.Name ?? "None"}, Visible: {IsVisible})";

    public override void Dispose()
    {
        RootComponent?.Dispose();
    }
}
