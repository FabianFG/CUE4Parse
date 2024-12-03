using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Versions;
using Newtonsoft.Json;

namespace CUE4Parse.UE4.Assets.Exports.Actor;

public class AActor : UObject
{
    public bool bIsCooked;
    public string? ActorLabel;
    public FActorInstanceGuid? ActorInstanceGuid;

    public override void Deserialize(FAssetArchive Ar, long validPos)
    {
        base.Deserialize(Ar, validPos);

        if (FUE5PrivateFrostyStreamObjectVersion.Get(Ar) >= FUE5PrivateFrostyStreamObjectVersion.Type.SerializeActorLabelInCookedBuilds)
        {
            bIsCooked = Ar.ReadBoolean();
            if (bIsCooked)
                ActorLabel = Ar.ReadFString();
        }

        if (FFortniteMainBranchObjectVersion.Get(Ar) >= FFortniteMainBranchObjectVersion.Type.LevelInstanceStaticLightingSupport)
        {
            ActorInstanceGuid = new FActorInstanceGuid(Ar);
        }
    }
}
