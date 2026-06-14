using System;
using System.Collections.Generic;
using CUE4Parse.UE4.Assets.Exports.Component;
using CUE4Parse.UE4.Assets.Exports.Component.Landscape;
using CUE4Parse.UE4.Assets.Exports.Component.Lights;
using CUE4Parse.UE4.Assets.Exports.Component.SkeletalMesh;
using CUE4Parse.UE4.Assets.Exports.Component.SplineMesh;
using CUE4Parse.UE4.Assets.Exports.Component.StaticMesh;
using CUE4Parse.UE4.Objects.UObject;
using Serilog;

namespace CUE4Parse_Conversion.Dto;

internal sealed class WorldParseContext
{
    /// <summary>
    /// flattened list of resolved components keyed by their unresolved pointer
    /// </summary>
    private readonly Dictionary<FPackageIndex, ComponentDto> _registry = new();

    public ComponentDto? GetOrCreate(FPackageIndex? ptr, ActorDto owner)
    {
        if (ptr is not { IsNull: false }) return null;
        if (_registry.TryGetValue(ptr, out var existing)) return existing;

        var dto = CreateDto(ptr, owner);
        if (dto is null) return null;

        _registry[ptr] = dto;

        if (dto is SceneComponentDto { _attachParent: { IsNull: false } parentPtr } scene && GetOrCreate(parentPtr, owner) is SceneComponentDto parentScene)
        {
            parentScene.AddChildComponent(scene);
        }

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
                    UStaticMeshComponent sm when sm.TryGetValue<FPackageIndex>(out var mesh, "StaticMesh") => sm switch
                    {
                        UInstancedStaticMeshComponent ism => new InstancedStaticMeshComponentDto(mesh, ism, owner),
                        USplineMeshComponent spline => new SplineMeshComponentDto(mesh, spline, owner),
                        _ => new StaticMeshComponentDto(mesh, sm, owner)
                    },
                    USkeletalMeshComponent sk when sk.TryGetValue<FPackageIndex>(out var mesh, "SkeletalMesh", "SkinnedAsset") => new SkeletalMeshComponentDto(mesh, sk, owner),
                    ULandscapeComponent landscape => new LandscapeMeshComponentDto(landscape, owner),
                    ULandscapeSplinesComponent splines => new LandscapeSplinesComponentDto(splines, owner),
                    // UBillboardComponent billboard => new BillboardComponent(billboard),
                    // UArrowComponent arrow => new ArrowComponent(arrow),
                    // UBrushComponent brush => new BrushComponentDto(brush, owner),
                    // UShapeComponent shape when shape.Outer?.Object?.Value is not ALevelBounds => shape switch
                    // {
                    //     UBoxComponent box => new BoxComponentDto(box, owner),
                    //     USphereComponent sphere => new SphereComponentDto(sphere, owner),
                    //     UCapsuleComponent capsule => new CapsuleComponentDto(capsule, owner),
                    //     _ => new SceneComponentDto(shape, owner)
                    // },
                    ULightComponentBase light => light switch
                    {
                        USpotLightComponent spotLight => new SpotLightComponentDto(spotLight, owner),
                        UPointLightComponent pointLight => new PointLightComponentDto(pointLight, owner),
                        URectLightComponent rectLight => new RectLightComponentDto(rectLight, owner),
                        UDirectionalLightComponent directionalLight => new DirectionalLightComponentDto(directionalLight, owner),
                        _ => new SceneComponentDto(light, owner)
                    },
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
}
