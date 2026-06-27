using CUE4Parse.UE4.Assets.Exports;
using CUE4Parse.UE4.Assets.Exports.Actor;
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
            {
                RootComponent = root;
            }
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
        _ => new ActorDto(actor, ctx)
    };

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

    public override string ToString() => $"{base.ToString()} (RootComponent: {RootComponent?.Name ?? "None"}, Visible: {IsVisible})";

    public override void Dispose()
    {
        RootComponent?.Dispose();
    }
}
