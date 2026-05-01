using System;
using System.Collections.Generic;
using CUE4Parse.UE4.Assets.Exports;
using CUE4Parse.UE4.Assets.Exports.Component;
using CUE4Parse.UE4.Assets.Exports.Component.SkeletalMesh;
using CUE4Parse.UE4.Assets.Exports.Component.StaticMesh;
using CUE4Parse.UE4.Objects.Core.Math;
using CUE4Parse.UE4.Objects.UObject;

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
    public readonly FPackageIndex?[] OverrideMaterials;

    protected MeshComponentDto(UMeshComponent component, ActorDto owner) : base(component, owner)
    {
        OverrideMaterials = component.OverrideMaterials;
    }
}

public class StaticMeshComponentDto : MeshComponentDto
{
    public readonly FPackageIndex StaticMesh;

    public StaticMeshComponentDto(UStaticMeshComponent component, ActorDto owner) : base(component, owner)
    {
        StaticMesh = component.Get<FPackageIndex>(nameof(StaticMesh));
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
    public readonly FPackageIndex SkinnedMesh;

    protected SkinnedMeshComponentDto(USkinnedMeshComponent component, ActorDto owner) : base(component, owner)
    {
        SkinnedMesh = component.GetOrDefault<FPackageIndex?>("SkeletalMesh") ?? component.Get<FPackageIndex>("SkinnedAsset");
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
