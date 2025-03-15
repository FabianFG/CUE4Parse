using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Objects.Core.Misc;

namespace CUE4Parse.UE4.Assets.Exports.Actor;

public class FActorInstanceGuid
{
    public FGuid ActorGuid;
    public FGuid ActorInstanceGuid;

    public FActorInstanceGuid(FAssetArchive Ar)
    {
        ActorGuid = Ar.Read<FGuid>();
        ActorInstanceGuid = Ar.Read<FGuid>();
    }
}
