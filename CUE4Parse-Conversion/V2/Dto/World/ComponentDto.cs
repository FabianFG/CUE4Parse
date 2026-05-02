using System;
using System.Collections.Generic;
using CUE4Parse.UE4.Assets.Exports;
using CUE4Parse.UE4.Assets.Exports.Actor;
using CUE4Parse.UE4.Assets.Exports.Component;
using CUE4Parse.UE4.Assets.Exports.Component.Landscape;
using CUE4Parse.UE4.Assets.Exports.Component.SkeletalMesh;
using CUE4Parse.UE4.Assets.Exports.Component.StaticMesh;
using CUE4Parse.UE4.Objects.Core.Math;
using CUE4Parse.UE4.Objects.UObject;
using CUE4Parse.Utils;

namespace CUE4Parse_Conversion.V2.Dto.World;

public class ComponentDto : ObjectDto
{
    public readonly ActorDto Owner;

    public ComponentDto(UObject component, ActorDto owner) : base(component)
    {
        Owner = owner;
    }

    public override void Dispose()
    {
        throw new NotImplementedException();
    }
}

public class SceneComponentDto : ComponentDto
{
    internal readonly FPackageIndex? _attachParent;
    public SceneComponentDto? Parent { get; private set; }

    public readonly List<SceneComponentDto> Children = [];
    public readonly FTransform Transform;
    public readonly string? AttachSocketName;

    internal void SetParent(SceneComponentDto parent) => Parent = parent;
    internal void AddChild(SceneComponentDto child) => Children.Add(child);

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
}

public abstract class MeshComponentDto : PrimitiveComponentDto
{
    public abstract FPackageIndex MeshPtr { get; }
    public readonly FPackageIndex?[] OverrideMaterials;

    protected MeshComponentDto(UMeshComponent component, ActorDto owner) : base(component, owner)
    {
        OverrideMaterials = component.OverrideMaterials;
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
    public readonly ULandscapeComponent Component;
    public readonly string Ref;

    public LandscapeMeshComponentDto(ULandscapeComponent component, ActorDto owner) : base(component, owner)
    {
        if (!PerComponentExport)
        {
            // find the component's parent actor
            component.Outer?.TryLoad<ALandscapeProxy>(out OuterProxy);
        }

        Component = component;
        Ref = $"./{component.Owner?.Name.SubstringAfterLast('/')}/{OuterProxy?.Name ?? component.Name}.usda"; // kinda sketchy
    }
}

public class StaticMeshComponentDto : MeshComponentDto
{
    public override FPackageIndex MeshPtr { get; }

    public StaticMeshComponentDto(UStaticMeshComponent component, ActorDto owner) : base(component, owner)
    {
        MeshPtr = component.Get<FPackageIndex>(nameof(StaticMesh));
    }
}

public class InstancedStaticMeshComponentDto : StaticMeshComponentDto
{
    public readonly FTransform[] Transforms;

    public InstancedStaticMeshComponentDto(UInstancedStaticMeshComponent component, ActorDto owner) : base(component, owner)
    {
        var instances = component.GetInstances();
        Transforms = new FTransform[instances.Length];
        for (var i = 0; i < Transforms.Length; i++)
        {
            Transforms[i] = instances[i].TransformData;
        }
    }
}

public abstract class SkinnedMeshComponentDto : MeshComponentDto
{
    public override FPackageIndex MeshPtr { get; }

    protected SkinnedMeshComponentDto(USkinnedMeshComponent component, ActorDto owner) : base(component, owner)
    {
        MeshPtr = component.GetOrDefault<FPackageIndex?>("SkeletalMesh") ?? component.Get<FPackageIndex>("SkinnedAsset");
    }
}

public class SkeletalMeshComponentDto : SkinnedMeshComponentDto
{
    public readonly FSingleAnimationPlayData? AnimationData;

    public SkeletalMeshComponentDto(USkeletalMeshComponent component, ActorDto owner) : base(component, owner)
    {
        AnimationData = component.AnimationData;
    }
}

public abstract class ShapeComponentDto : PrimitiveComponentDto
{
    protected const float DefaultRadius = 0.5f;

    public readonly FColor ShapeColor;

    protected ShapeComponentDto(UShapeComponent component, ActorDto owner) : base(component, owner)
    {
        ShapeColor = component.GetOrDefault<FColor>(nameof(ShapeColor));
    }
}

public class BoxComponentDto : ShapeComponentDto
{
    public readonly FVector BoxExtent;

    public BoxComponentDto(UBoxComponent component, ActorDto owner) : base(component, owner)
    {
        BoxExtent = component.GetOrDefault(nameof(BoxExtent), FVector.OneVector * DefaultRadius);
    }
}

public class SphereComponentDto : ShapeComponentDto
{
    public readonly float SphereRadius;

    public SphereComponentDto(USphereComponent component, ActorDto owner) : base(component, owner)
    {
        SphereRadius = component.GetOrDefault(nameof(SphereRadius), DefaultRadius);
    }
}

public class CapsuleComponentDto : ShapeComponentDto
{
    public readonly float CapsuleHalfHeight;
    public readonly float CapsuleRadius;

    public CapsuleComponentDto(UCapsuleComponent component, ActorDto owner) : base(component, owner)
    {
        CapsuleHalfHeight = component.GetOrDefault(nameof(CapsuleHalfHeight), DefaultRadius * 2.3f);
        CapsuleRadius = component.GetOrDefault(nameof(CapsuleRadius), DefaultRadius);
    }
}

public class BrushComponentDto : PrimitiveComponentDto
{
    public readonly FPackageIndex BrushPtr;

    public BrushComponentDto(UBrushComponent component, ActorDto owner) : base(component, owner)
    {
        BrushPtr = component.Brush ?? throw new InvalidOperationException("Component does not have a Brush");
    }
}
