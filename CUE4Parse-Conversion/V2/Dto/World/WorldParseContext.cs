using System;
using System.Collections.Generic;
using CUE4Parse.UE4.Assets.Exports.Component;
using CUE4Parse.UE4.Assets.Exports.Component.SkeletalMesh;
using CUE4Parse.UE4.Assets.Exports.Component.StaticMesh;
using CUE4Parse.UE4.Objects.UObject;
using Serilog;

namespace CUE4Parse_Conversion.V2.Dto.World;

internal sealed class WorldParseContext
{
    /// <summary>
    /// flattened list of resolved components keyed by their unresolved pointer
    /// </summary>
    private readonly Dictionary<FPackageIndex, ComponentDto> _registry = new();

    /// <summary>
    /// Returns the existing ComponentDto for <paramref name="ptr"/> if already
    /// registered, otherwise loads the UObject, constructs the appropriate
    /// ComponentDto subtype, registers it, and returns it.
    /// Returns null when the pointer is null/zero or fails to load.
    /// </summary>
    public ComponentDto? GetOrCreate(FPackageIndex? ptr, ActorDto owner)
    {
        if (ptr is not { IsNull: false }) return null;

        if (_registry.TryGetValue(ptr, out var existing))
            return existing;

        var dto = CreateDto(ptr, owner);
        if (dto is null) return null;

        _registry[ptr] = dto;
        return dto;
    }

    private ComponentDto? CreateDto(FPackageIndex ptr, ActorDto owner)
    {
        try
        {
            return ptr.Load() switch
            {
                USceneComponent scene => scene switch
                {
                    UInstancedStaticMeshComponent ism => new InstancedStaticMeshComponentDto(ism, owner),
                    // USplineMeshComponent spline => new SplineMeshComponentDto(spline),
                    UStaticMeshComponent sm => new StaticMeshComponentDto(sm, owner),
                    USkeletalMeshComponent sk => new SkeletalMeshComponentDto(sk, owner),
                    // ULandscapeComponent landscape => new LandscapeMeshComponent(landscape),
                    // ULandscapeSplinesComponent splines => new LandscapeSplinesComponent(splines),
                    // UBillboardComponent billboard => new BillboardComponent(billboard),
                    // UArrowComponent arrow => new ArrowComponent(arrow),
                    // UBrushComponent brushComponent when brushComponent.GetBrush() is { } brush => new BrushComponent(brushComponent, brush),
                    // UShapeComponent shape when shape.Outer?.Object?.Value is not ALevelBounds => shape switch // exclude level bounds because their scale looks weird and overall they provide little value
                    // {
                    //     UBoxComponent box => new BoxComponent(box),
                    //     USphereComponent sphere => new SphereComponent(sphere),
                    //     UCapsuleComponent capsule => new CapsuleComponent(capsule),
                    //     _ => new SpatialComponent(shape)
                    // },
                    // ULightComponentBase light => light switch
                    // {
                    //     USpotLightComponent spotLight => new SpotLightComponent(spotLight),
                    //     UPointLightComponent pointLight => new PointLightComponent(pointLight),
                    //     URectLightComponent rectLight => new RectLightComponent(rectLight),
                    //     UDirectionalLightComponent directionalLight => new DirectionalLightComponent(directionalLight),
                    //     _ => new SpatialComponent(light)
                    // },
                    // UAudioComponent audio => new AudioComponent(audio),
                    // UTextRenderComponent text => new TextRenderComponent(text),
                    // UCameraComponent camera => new CameraComponent(camera),
                    _ => new SceneComponentDto(scene, owner),
                },
                { } data => new ComponentDto(data, owner),
                null => null,
            };
        }
        catch (Exception e)
        {
            Log.Error(e, "Failed to create ComponentDto for pointer {Ptr}", ptr);
            return null;
        }
    }

    /// <summary>
    /// Resolves every SceneComponentDto's AttachParentPtr against the registry
    /// and populates Parent/Children links.  Also detects cross-actor attachment
    /// (an actor's root component whose parent lives in a different actor) and
    /// populates ActorDto.ChildActors accordingly.
    /// Must be called exactly once, after all actors/components are registered.
    /// </summary>
    public void WireHierarchy()
    {
        foreach (var component in _registry.Values)
        {
            if (component is not SceneComponentDto { _attachParent: { IsNull: false } parentPtr } scene)
                continue;

            if (!_registry.TryGetValue(parentPtr, out var parentComponent) || parentComponent is not SceneComponentDto parentScene)
                continue;

            scene.SetParent(parentScene);
            parentScene.AddChild(scene);

            // Cross-actor attachment: actor B's root component's parent lives in actor A
            // → actor B is a child actor of actor A
            if (parentComponent.Owner != component.Owner && component.Owner.RootComponent == scene)
            {
                parentComponent.Owner.AddChildActor(component.Owner);
            }
        }
    }
}
