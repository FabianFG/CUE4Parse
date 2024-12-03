using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Objects.Core.Misc;
using CUE4Parse.UE4.Objects.UObject;
using CUE4Parse.UE4.Versions;
using Newtonsoft.Json;

namespace CUE4Parse.UE4.Assets.Exports.Component;

public class UActorComponent : UObject
{
    [JsonIgnore] public FSimpleMemberReference[]? UCSModifiedProperties;

    public override void Deserialize(FAssetArchive Ar, long validPos)
    {
        base.Deserialize(Ar, validPos);
        if (Ar.Position == validPos) // I think after validpos all read default to dummy data 000000s
            return;

        if (FFortniteReleaseBranchCustomObjectVersion.Get(Ar) >= FFortniteReleaseBranchCustomObjectVersion.Type.ActorComponentUCSModifiedPropertiesSparseStorage)
        {
            UCSModifiedProperties = Ar.ReadArray(() => new FSimpleMemberReference(Ar));
        }
    }
}

public struct FSimpleMemberReference(FAssetArchive Ar)
{
    public FPackageIndex MemberParent = new FPackageIndex(Ar);
    public FName MemberName = Ar.ReadFName();
    public FGuid MemberGuid = Ar.Read<FGuid>();
}
