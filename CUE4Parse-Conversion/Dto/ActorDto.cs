using CUE4Parse.GameTypes.OtherGames.Objects;
using CUE4Parse.UE4.Assets.Exports;
using CUE4Parse.UE4.Assets.Exports.Actor;
using CUE4Parse.UE4.Assets.Exports.Component;
using CUE4Parse.UE4.Objects.Core.Math;
using CUE4Parse.UE4.Objects.Engine;
using CUE4Parse.UE4.Objects.UObject;

namespace CUE4Parse_Conversion.Dto;

public class ActorDto : ObjectDto
{
    public SceneComponentDto? RootComponent { get; protected init; }
    public List<StreamingLevel>? StreamingLevels { get; protected init; }
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
                RootComponent = ctx.GetRoot(root);
        }

        if (actor.TryGetValue(out bool hidden, "bHidden"))
        {
            IsVisible = !hidden;
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
        AGeneratedMeshActor gma => new GeneratedMeshActorDto(gma, ctx),
        _ => new ActorDto(actor, ctx)
    };

    public bool HasStreamingLevels()
    {
        if (StreamingLevels is { Count: > 0 }) return true;
        return RootComponent?.HasStreamingLevels() ?? false;
    }

    private IEnumerable<FPackageIndex?> FindComponents(UObject actor)
    {
        var components = new List<FPackageIndex>();
        var seen = new HashSet<FPackageIndex>();

        // the root drives the attachment hierarchy, so it has to come first
        if (actor.TryGetValue(out FPackageIndex rootComponent, "RootComponent"))
            Collect(rootComponent);

        foreach (var property in actor.Properties)
        {
            if (actor.TryGetValue(out FPackageIndex component, property.Name.Text))
                Collect(component);
            if (actor.TryGetValue(out FPackageIndex[] componentArray, property.Name.Text))
                foreach (var arrayComponent in componentArray)
                    Collect(arrayComponent);
        }

        foreach (var component in components)
            yield return component;

        if (actor is AInstancedFoliageActor { FoliageInfos: { } foliages })
        {
            foreach (var foliage in foliages.Values)
            {
                switch (foliage.Implementation)
                {
                    case FFoliageStaticMesh staticMesh:
                        yield return staticMesh.Component;
                        break;
                    // case FFoliageActor: // FFoliageActor.ActorInstances seem to be included in InstanceComponents already
                    //     throw new NotImplementedException("FoliageActor is not supported yet");
                }
            }
        }

        void Collect(FPackageIndex? ptr)
        {
            // components live in the same package as their actor, imports point at unrelated assets
            if (ptr is { IsNull: false, IsExport: true } && seen.Add(ptr) && ptr.TryLoad(out UActorComponent _))
                components.Add(ptr);
        }
    }

    public override string ToString() => $"{base.ToString()} (RootComponent: {RootComponent?.Name ?? "None"}, Visible: {IsVisible})";

    public override void Dispose()
    {
        RootComponent?.Dispose();
    }
}

// Minecraft Dungeons 2
public class GeneratedMeshActorDto : ActorDto
{
    internal GeneratedMeshActorDto(AGeneratedMeshActor generatedMeshActor, WorldParseContext ctx) : base(generatedMeshActor)
    {
        RootComponent = new SceneComponentDto(FTransform.Identity, "SceneComponent", this);

        foreach (var component in generatedMeshActor.ComponentsSplit.Values)
        {
            if (!component.TryLoad<UGeneratedMeshActorComponentSplit>(out var componentSplit)) continue;
            foreach (var subMesh in componentSplit.SubMeshes)
            {
                var c = ctx.GetOrCreate(subMesh, this);
                if (c is SceneComponentDto scd)
                    RootComponent.AddChildComponent(scd);
            }
        }
    }
}
