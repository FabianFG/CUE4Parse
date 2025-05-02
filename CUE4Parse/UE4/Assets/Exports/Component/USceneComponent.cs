using System;
using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Objects.Core.Math;
using CUE4Parse.UE4.Objects.UObject;
using CUE4Parse.UE4.Versions;
using Newtonsoft.Json;

namespace CUE4Parse.UE4.Assets.Exports.Component;


public enum ERelativeTransformSpace : int
{
    /** World space transform. */
    RTS_World,
    /** Actor space transform. */
    RTS_Actor,
    /** Component space transform. */
    RTS_Component,
    /** Parent bone space transform */
    RTS_ParentBoneSpace,
};


public class USceneComponent : UActorComponent
{
    public FBoxSphereBounds? Bounds;
    public bool bIsCooked;

    public override void Deserialize(FAssetArchive Ar, long validPos)
    {
        base.Deserialize(Ar, validPos);

        var bComputeBoundsOnceForGame = GetOrDefault<bool>("bComputeBoundsOnceForGame");
        var bComputedBoundsOnceForGame = GetOrDefault<bool>("bComputedBoundsOnceForGame");
        var bComputeBounds = bComputeBoundsOnceForGame || bComputedBoundsOnceForGame;
        if (bComputeBounds && FUE5SpecialProjectStreamObjectVersion.Get(Ar) >= FUE5SpecialProjectStreamObjectVersion.Type.SerializeSceneComponentStaticBounds)
        {
            bIsCooked = Ar.ReadBoolean();
            if (bIsCooked)
                Bounds = new FBoxSphereBounds(Ar);
        }
    }

    public FTransform GetAbsoluteTransform()
    {
        var newTransform = new FTransform(GetRelativeRotation(), GetRelativeLocation(), GetRelativeScale3D());
        var parent = GetAttachParent();
        while (parent != null)
        {
            newTransform = newTransform * parent!.GetSocketTransform("", ERelativeTransformSpace.RTS_World);
            parent = parent.GetAttachParent();
        }
        return newTransform;
    }

    private FTransform GetComponentToWorld()
    {
        var relativeTransform = new FTransform(GetRelativeRotation(), GetRelativeLocation(), GetRelativeScale3D());
        if (GetAttachParent() != null) // CalcNewComponentToWorld_GeneralCases
        {
            return relativeTransform * GetAttachParent()!.GetSocketTransform("", ERelativeTransformSpace.RTS_World);
        }

        return relativeTransform;
    }

    public FTransform GetSocketTransform(string socketName, ERelativeTransformSpace transformSpace)
    {
        var relativeTransform = new FTransform(GetRelativeRotation(), GetRelativeLocation(), GetRelativeScale3D());
        if (transformSpace == ERelativeTransformSpace.RTS_World)
        {
            return relativeTransform;
        }
        else
        {
            throw new NotImplementedException();
            // return relativeTransform * GetComponentTransform();
        }
    }

    public USceneComponent? GetAttachParent()
    {
        return GetOrDefault<FPackageIndex?>("AttachParent")?.Load<USceneComponent>();
    }

    public FTransform GetComponentTransform()
    {
        return GetComponentToWorld();
    }

    public FVector GetRelativeLocation()
    {
        return GetOrDefault<FVector>("RelativeLocation");
    }

    public FRotator GetRelativeRotation()
    {
        return GetOrDefault("RelativeRotation", FRotator.ZeroRotator);
    }

    public FVector GetRelativeScale3D()
    {
        return GetOrDefault("RelativeScale3D", FVector.OneVector);
    }

    protected internal override void WriteJson(JsonWriter writer, JsonSerializer serializer)
    {
        base.WriteJson(writer, serializer);

        if (Bounds is null) return;
        writer.WritePropertyName("Bounds");
        serializer.Serialize(writer, Bounds);
    }
}
