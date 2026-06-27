using CUE4Parse.UE4;
using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Objects.Core.Math;
using CUE4Parse.UE4.Objects.Core.Misc;
using CUE4Parse.UE4.Objects.UObject;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace CUE4Parse.GameTypes.FN.Objects;

public class FFortActorRecord : IUStruct
{
    [JsonConverter(typeof(StringEnumConverter))]
    public enum EFortBuildingPersistentState
    {
        Default,
        New,
        Constructed,
        Destroyed,
        Searched,
        None,
        MAX
    }

    public readonly FGuid ActorGuid;
    public readonly EFortBuildingPersistentState ActorState;
    public readonly FPackageIndex ActorClass;
    public readonly FTransform Transform;
    public readonly bool bSpawnedActor;
    public readonly byte[] ActorData;

    public FFortActorRecord(FAssetArchive Ar)
    {
        ActorGuid = Ar.Read<FGuid>();

        ActorState = (EFortBuildingPersistentState)Ar.Read<byte>();

        // ActorClass is sometimes saved as FPackageIndex
        ActorClass = new FPackageIndex(Ar);
        Transform = new FTransform(Ar);
        bSpawnedActor = Ar.ReadBoolean();

        // skip 1 byte then do FPropertyTag, i've tried but keeps causing odd issues.
        ActorData = Ar.ReadArray<byte>();
    }
}
