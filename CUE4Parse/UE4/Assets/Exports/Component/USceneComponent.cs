using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Objects.Core.Math;
using CUE4Parse.UE4.Versions;
using Newtonsoft.Json;

namespace CUE4Parse.UE4.Assets.Exports.Component;

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
        if (bComputeBounds && FUE5PrivateFrostyStreamObjectVersion.Get(Ar) >= FUE5PrivateFrostyStreamObjectVersion.Type.SerializeSceneComponentStaticBounds)
        {
            bIsCooked = Ar.ReadBoolean();
            if (bIsCooked)
                Bounds = new FBoxSphereBounds(Ar);
        }
    }

    protected internal override void WriteJson(JsonWriter writer, JsonSerializer serializer)
    {
        base.WriteJson(writer, serializer);

        if (Bounds is null) return;
        writer.WritePropertyName("Bounds");
        serializer.Serialize(writer, Bounds);
    }
}

public class UArrowComponent : USceneComponent;
public class UAudioComponent : USceneComponent;
public class UBillboardComponent : USceneComponent;
public class UBoxComponent : USceneComponent;
public class UBrushComponent : USceneComponent;
public class UCameraComponent : USceneComponent;
public class UCapsuleComponent : USceneComponent;
public class UDecalComponent : USceneComponent;
public class USplineComponent : USceneComponent;
public class UTextRenderComponent : USceneComponent;
