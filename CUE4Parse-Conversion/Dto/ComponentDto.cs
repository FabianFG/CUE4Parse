using CUE4Parse.UE4.Assets.Exports;
using CUE4Parse.UE4.Assets.Exports.Actor;
using CUE4Parse.UE4.Assets.Exports.Component;
using CUE4Parse.UE4.Assets.Exports.Component.Landscape;
using CUE4Parse.UE4.Assets.Exports.Component.Lights;
using CUE4Parse.UE4.Assets.Exports.Component.SkeletalMesh;
using CUE4Parse.UE4.Assets.Exports.Component.SplineMesh;
using CUE4Parse.UE4.Assets.Exports.Component.StaticMesh;
using CUE4Parse.UE4.Assets.Exports.GeometryCollection;
using CUE4Parse.UE4.Objects.Core.Math;
using CUE4Parse.UE4.Objects.Engine;
using CUE4Parse.UE4.Objects.UObject;

namespace CUE4Parse_Conversion.Dto;

public class ComponentDto : LightObjectDto
{
    public readonly ActorDto Owner;

    protected ComponentDto(string name, ActorDto owner) : base(name)
    {
        Owner = owner;
    }

    public ComponentDto(UObject component, ActorDto owner) : base(component)
    {
        Owner = owner;
    }

    public override void Dispose()
    {

    }
}

public class SceneComponentDto : ComponentDto
{
    internal readonly FPackageIndex? _attachParent;

    public readonly List<SceneComponentDto> Children = []; // same actor components only
    public readonly List<ActorDto> AttachedActors = []; // actors whose root is attached here

    public FTransform Transform;
    public readonly string? AttachSocketName; // TODO: use it

    internal void AddChildComponent(SceneComponentDto child)
    {
        if (child.Owner != Owner)
            AttachedActors.Add(child.Owner);
        else
            Children.Add(child);
    }

    public SceneComponentDto(FTransform transform, string name, ActorDto owner) : base(name, owner)
    {
        Transform = transform;
    }

    public SceneComponentDto(USceneComponent component, ActorDto owner) : base(component, owner)
    {
        _attachParent = component.AttachParent;
        Transform = component.GetRelativeTransform();
        AttachSocketName = component.GetOrDefault<FName?>(nameof(AttachSocketName))?.Text;

        if (component.GetOrDefault<bool>("bAbsoluteLocation"))
        {
            // TODO
        }
        if (component.GetOrDefault<bool>("bAbsoluteRotation"))
        {
            // TODO
        }
        if (component.GetOrDefault<bool>("bAbsoluteScale"))
        {
            // TODO
        }
    }

    public bool HasStreamingLevels()
    {
        foreach (var actor in AttachedActors)
            if (actor.HasStreamingLevels())
                return true;

        foreach (var component in Children)
            if (component.HasStreamingLevels())
                return true;

        return false;
    }

    public override void Dispose()
    {
        base.Dispose();

        foreach (var child in Children)
        {
            child.Dispose();
        }
        Children.Clear();
    }
}

public abstract class PrimitiveComponentDto : SceneComponentDto
{
    public readonly bool IsVisible = true;
    public readonly bool CastShadow = true;

    protected PrimitiveComponentDto(UPrimitiveComponent component, ActorDto owner) : base(component, owner)
    {
        if (component.TryGetValue(out bool visible, "bVisible"))
        {
            IsVisible = visible;
        }
        else if (component.TryGetValue(out bool hidden, "bHiddenInGame"))
        {
            IsVisible = !hidden;
        }

        if (component.TryGetValue(out bool castShadow, "CastShadow", "bCastStaticShadow", "bCastDynamicShadow"))
        {
            CastShadow = castShadow;
        }
    }

    protected PrimitiveComponentDto(FTransform transform, string name, ActorDto owner) : base(transform, name, owner)
    {

    }
}

/// <summary>
/// The mesh pointer is resolved by the caller and passed in rather than being looked up inside the ctor
/// This is intentional because we want to enforce the fact that a <see cref="MeshComponentDto"/> contains a mesh (even tho technically you can pass in any pointer)
/// if no mesh pointer was found the caller should fall back to <see cref="SceneComponentDto"/> so that child components are not lost
/// <para>
/// Example hierarchy where <c>Wire</c> has no mesh assigned but still parents two spline meshes:
/// <code>
/// BP_Actor
/// └─ Root (SceneComponent)
///    └─ Wire (StaticMeshComponent, no mesh)
///       ├─ Spline1 (SplineMeshComponent)
///       └─ Spline2 (SplineMeshComponent)
/// </code>
/// If the constructor threw on a missing mesh, <c>Spline1</c> and <c>Spline2</c> would never be visited.
/// </para>
/// </summary>
public abstract class MeshComponentDto : PrimitiveComponentDto
{
    public readonly FPackageIndex MeshPtr;
    public readonly FPackageIndex?[] OverrideMaterials;

    protected MeshComponentDto(FPackageIndex meshPtr, UMeshComponent component, ActorDto owner) : base(component, owner)
    {
        MeshPtr = meshPtr;
        OverrideMaterials = component.OverrideMaterials;
    }

    protected MeshComponentDto(FPackageIndex meshPtr, ActorDto owner) : base(FTransform.Identity, meshPtr.Name, owner)
    {
        MeshPtr = meshPtr;
        OverrideMaterials = [];
    }
}

/// <summary>
/// <para>
/// <b>TODO:</b> landscape can be exported two ways, each with trade-offs:
/// <list type="bullet">
///   <item><b>Per-component:</b> each <see cref="ULandscapeComponent"/> is written as its own
///   file (~3 MB each for <c>.usda</c>; 50/100+ per world), which allows the world to reference individual
///   components and preserves per-component mobility. However it is expensive and each file is only a small
///   tile of the full landscape.</item>
///   <item><b>Per-actor:</b> the standalone landscape exporter merges all components into a single mesh with
///   combined normal/weightmap textures but it cannot be referenced back into the
///   world, and we loose component-level granularity.</item>
/// </list>
/// </para>
/// </summary>
public class LandscapeMeshComponentDto : PrimitiveComponentDto
{
    public const bool PerComponentExport = true;

    public readonly ALandscapeProxy? OuterProxy;
    internal readonly ULandscapeComponent _component;

    public LandscapeMeshComponentDto(ULandscapeComponent component, ActorDto owner) : base(component, owner)
    {
        if (!PerComponentExport)
        {
            // find the component's parent actor
            component.Outer?.TryLoad<ALandscapeProxy>(out OuterProxy);
        }

        _component = component;
    }
}

public class StaticMeshComponentDto : MeshComponentDto
{
    public StaticMeshComponentDto(FPackageIndex meshPtr, UStaticMeshComponent component, ActorDto owner) : base(meshPtr, component, owner)
    {

    }

    protected StaticMeshComponentDto(FPackageIndex meshPtr, ActorDto owner) : base(meshPtr, owner)
    {

    }
}

public class InstancedStaticMeshComponentDto : StaticMeshComponentDto
{
    public readonly FTransform[] Transforms;

    public InstancedStaticMeshComponentDto(FPackageIndex meshPtr, UInstancedStaticMeshComponent component, ActorDto owner) : base(meshPtr, component, owner)
    {
        var instances = component.GetInstances();
        Transforms = new FTransform[instances.Length];
        for (var i = 0; i < Transforms.Length; i++)
        {
            Transforms[i] = instances[i].TransformData;
        }
    }

    public InstancedStaticMeshComponentDto(FPackageIndex meshPtr, FTransform[] transforms, ActorDto owner) : base(meshPtr, owner)
    {
        Transforms = transforms;
    }
}

public class SplineMeshComponentDto(FPackageIndex meshPtr, USplineMeshComponent component, ActorDto owner) : StaticMeshComponentDto(meshPtr, component, owner)
{
    internal readonly USplineMeshComponent _component = component;
}

public class GeometryCollectionComponentDto : MeshComponentDto
{
    public GeometryCollectionComponentDto(FPackageIndex meshPtr, UGeometryCollectionComponent component, ActorDto owner) : base(meshPtr, component, owner)
    {
        if (!meshPtr.TryLoad<UGeometryCollection>(out var geometryCollection)) return;
        if (geometryCollection is not { AutoInstanceMeshes: { Length: > 0 } meshes, GeometryCollection: { } collection }) return;

        // TODO: we should use the vertices group to create this component actual mesh data
        // stuff from other groups and especially AutoInstanceMeshes are just relation of this component but they don't define its geometry (at least for us)
        // we need an asset with actual vertices as an example, could not find any yet
        // there's also some kind of bone relation that we fully ignore here but it might actually be important

        var group = new FName("Transform");
        if (!collection.TryGetAttributeValue<int>("AutoInstanceMeshIndex", group, out var meshIndices) ||
            !collection.TryGetAttributeValue<FTransform>("Transform", group, out var transforms)) return;

        const int rigid = 1;
        var simulationTypes = collection.GetAttributeValue<int>("SimulationType", group);
        var parents = collection.GetAttributeValue<int>("Parent", group);
        var hides = collection.GetAttributeValue<bool>("Hide", group);

        var placements = new List<FTransform>?[meshes.Length];
        for (var i = 0; i < transforms.Length; i++)
        {
            if (GetIndex(simulationTypes, i, rigid) != rigid || GetIndex(hides, i, false)) continue;

            var meshIndex = GetIndex(meshIndices, i, -1);
            if (meshIndex < 0 || meshIndex >= meshes.Length) continue;

            (placements[meshIndex] ??= new List<FTransform>(meshes[meshIndex].NumInstances)).Add(ResolveTransform(i));
        }

        for (var i = 0; i < meshes.Length; i++)
        {
            if (placements[i] is not { Count: > 0 } instances || meshes[i].Mesh is not { } ptr) continue;
            Children.Add(new InstancedStaticMeshComponentDto(ptr, instances.ToArray(), owner));
        }

        FTransform ResolveTransform(int index)
        {
            var local = transforms[index];
            var parent = GetIndex(parents, index, -1);
            return parent >= 0 && parent < transforms.Length ? local * ResolveTransform(parent) : local;
        }
        T GetIndex<T>(T[]? data, int index, T fallback) => data is not null && index < data.Length ? data[index] : fallback;
    }
}

public class LandscapeSplinesComponentDto : PrimitiveComponentDto
{
    public LandscapeSplinesComponentDto(ULandscapeSplinesComponent component, ActorDto owner) : base(component, owner)
    {
        foreach (var ptr in component.Segments)
        {
            if (ptr?.TryLoad<ULandscapeSplineSegment>(out var segment) == true)
            {
                foreach (var meshPtr in segment.LocalMeshComponents)
                {
                    if (meshPtr?.TryLoad<USplineMeshComponent>(out var splineMesh) == true && splineMesh.TryGetValue<FPackageIndex>(out var mesh, "StaticMesh"))
                    {
                        Children.Add(new SplineMeshComponentDto(mesh, splineMesh, owner));
                    }
                }
            }
        }
    }
}

public abstract class SkinnedMeshComponentDto(FPackageIndex meshPtr, USkinnedMeshComponent component, ActorDto owner) : MeshComponentDto(meshPtr, component, owner);

public class SkeletalMeshComponentDto(FPackageIndex meshPtr, USkeletalMeshComponent component, ActorDto owner) : SkinnedMeshComponentDto(meshPtr, component, owner)
{
    public readonly FSingleAnimationPlayData? AnimationData = component.AnimationData;
}

public abstract class ShapeComponentDto(UShapeComponent component, ActorDto owner) : PrimitiveComponentDto(component, owner)
{
    protected const float DefaultRadius = 0.5f;

    public readonly FColor ShapeColor = component.GetOrDefault<FColor>(nameof(ShapeColor));
}

public class BoxComponentDto(UBoxComponent component, ActorDto owner) : ShapeComponentDto(component, owner)
{
    public readonly FVector BoxExtent = component.GetOrDefault(nameof(BoxExtent), FVector.OneVector * DefaultRadius);
}

public class SphereComponentDto(USphereComponent component, ActorDto owner) : ShapeComponentDto(component, owner)
{
    public readonly float SphereRadius = component.GetOrDefault(nameof(SphereRadius), DefaultRadius);
}

public class CapsuleComponentDto(UCapsuleComponent component, ActorDto owner) : ShapeComponentDto(component, owner)
{
    public readonly float CapsuleHalfHeight = component.GetOrDefault(nameof(CapsuleHalfHeight), DefaultRadius * 2.3f);
    public readonly float CapsuleRadius = component.GetOrDefault(nameof(CapsuleRadius), DefaultRadius);
}

public class BrushComponentDto(UBrushComponent component, ActorDto owner) : PrimitiveComponentDto(component, owner)
{
    public readonly FPackageIndex BrushPtr = component.Brush ?? throw new InvalidOperationException("Component does not have a Brush");
}

public abstract class LightComponentBaseDto(ULightComponentBase component, ActorDto owner) : SceneComponentDto(component, owner)
{
    public readonly float Intensity = component.Intensity;
    public readonly FLinearColor Color = component.GetLightColor();
    public readonly bool CastShadows = component.CastShadows;
}

public abstract class LightComponentDto(ULightComponent component, ActorDto owner) : LightComponentBaseDto(component, owner)
{
    public readonly float Temperature = component.Temperature;
    public readonly bool UseTemperature = component.bUseTemperature;
    public readonly float IntensityNits = component.GetNitIntensity();
}

public abstract class LocalLightComponentDto(ULocalLightComponent component, ActorDto owner) : LightComponentDto(component, owner)
{
    public readonly float AttenuationRadius = component.AttenuationRadius;
    public readonly ELightUnits IntensityUnits = component.GetLightUnits();
}

public class PointLightComponentDto(UPointLightComponent component, ActorDto owner) : LocalLightComponentDto(component, owner)
{
    public readonly float SourceRadius = component.SourceRadius;
    public readonly bool UseInverseSquaredFalloff = component.bUseInverseSquaredFalloff;
}

public class SpotLightComponentDto(USpotLightComponent component, ActorDto owner) : PointLightComponentDto(component, owner)
{
    public readonly float InnerConeAngle = component.InnerConeAngle;
    public readonly float OuterConeAngle = component.OuterConeAngle;
}

public class RectLightComponentDto(URectLightComponent component, ActorDto owner) : LocalLightComponentDto(component, owner)
{
    public readonly float SourceWidth = component.SourceWidth;
    public readonly float SourceHeight = component.SourceHeight;
}

public class DirectionalLightComponentDto(UDirectionalLightComponent component, ActorDto owner) : LightComponentDto(component, owner)
{
    public readonly float LightSourceAngle = component.LightSourceAngle;
}

public class SkyLightComponentDto(USkyLightComponent component, ActorDto owner) : LightComponentBaseDto(component, owner);
