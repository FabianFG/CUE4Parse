using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Objects.Core.Misc;
using CUE4Parse.UE4.Objects.UObject;
using CUE4Parse.UE4.Readers;
using CUE4Parse.UE4.Versions;
using Newtonsoft.Json;

namespace CUE4Parse.UE4.Assets.Exports.ControlRig;

public class UControlRig : UObject
{
    public FRigPhysicsSolverDescription[]? PhysicsSolvers;

    public override void Deserialize(FAssetArchive Ar, long validPos)
    {
        base.Deserialize(Ar, validPos);

        if (FControlRigObjectVersion.Get(Ar) >= FControlRigObjectVersion.Type.ControlRigStoresPhysicsSolvers)
        {
            PhysicsSolvers = Ar.ReadArray(() => new FRigPhysicsSolverDescription(Ar));
        }
    }

    protected internal override void WriteJson(JsonWriter writer, JsonSerializer serializer)
    {
        base.WriteJson(writer, serializer);

        if (PhysicsSolvers is null) return;
        writer.WritePropertyName(nameof(PhysicsSolvers));
        serializer.Serialize(writer, PhysicsSolvers);
    }
}

public struct FRigPhysicsSolverDescription(FArchive Ar)
{
    public FGuid ID = Ar.Read<FGuid>();
    public FName Name = Ar.ReadFName();
}
