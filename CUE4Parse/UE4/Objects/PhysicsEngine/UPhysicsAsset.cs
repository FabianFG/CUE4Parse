using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Objects.UObject;
using CUE4Parse.UE4.Readers;
using Newtonsoft.Json;

namespace CUE4Parse.UE4.Objects.PhysicsEngine;

public class UPhysicsAsset : Assets.Exports.UObject
{
    public int[] BoundsBodies;
    public FPackageIndex[] SkeletalBodySetups; // USkeletalBodySetup
    public FPackageIndex[] ConstraintSetup; // UPhysicsConstraintTemplate

    public Dictionary<FRigidBodyIndexPair, bool>? CollisionDisableTable;

    public override void Deserialize(FAssetArchive Ar, long validPos)
    {
        base.Deserialize(Ar, validPos);

        BoundsBodies = GetOrDefault(nameof(BoundsBodies), Array.Empty<int>());
        SkeletalBodySetups = GetOrDefault(nameof(SkeletalBodySetups), Array.Empty<FPackageIndex>());
        ConstraintSetup = GetOrDefault(nameof(ConstraintSetup), Array.Empty<FPackageIndex>());

        if (Ar.Game >= GAME_UE4_0)
        {
            CollisionDisableTable = Ar.ReadMap(() => new FRigidBodyIndexPair(Ar), Ar.ReadBoolean);
        }
    }

    protected internal override void WriteJson(JsonWriter writer, JsonSerializer serializer)
    {
        base.WriteJson(writer, serializer);

        writer.WritePropertyName(nameof(CollisionDisableTable));
        serializer.Serialize(writer, CollisionDisableTable);
    }
}

public class FRigidBodyIndexPair
{
    public readonly int[] Indices = new int[2];

    public FRigidBodyIndexPair(FArchive Ar)
    {
        Indices[0] = Ar.Read<int>();
        Indices[1] = Ar.Read<int>();
    }
};
