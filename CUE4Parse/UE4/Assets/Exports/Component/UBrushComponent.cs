using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Objects.Engine;
using CUE4Parse.UE4.Objects.PhysicsEngine;
using CUE4Parse.UE4.Objects.UObject;

namespace CUE4Parse.UE4.Assets.Exports.Component;

public class UBrushComponent : UPrimitiveComponent
{
    public FPackageIndex? Brush { get; protected set; }
    public FPackageIndex? BrushBodySetup { get; protected set; }

    public override void Deserialize(FAssetArchive Ar, long validPos)
    {
        base.Deserialize(Ar, validPos);

        Brush = GetOrDefault(nameof(Brush), new FPackageIndex());
        BrushBodySetup = GetOrDefault(nameof(BrushBodySetup), new FPackageIndex());
    }

    public UModel? GetBrush() => Brush?.Load<UModel>();
    public override UBodySetup? GetBodySetup() => BrushBodySetup?.Load<UBodySetup>();
}
