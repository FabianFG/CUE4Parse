using CUE4Parse.UE4;
using CUE4Parse.UE4.Objects.Core.Math;
using CUE4Parse.UE4.Objects.Core.Misc;
using CUE4Parse.UE4.Objects.UObject;
using CUE4Parse.UE4.Readers;

namespace CUE4Parse.GameTypes.FN.Objects
{
    public class FFortActorRecord : IUStruct
    {
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
        public readonly FName ActorClass;
        public readonly FTransform Transform;
        public readonly bool bSpawnedActor;
        public readonly byte[] ActorData;

        public FFortActorRecord(FArchive Ar)
        {
            ActorGuid = Ar.Read<FGuid>();
            
            ActorState = (EFortBuildingPersistentState)Ar.Read<byte>();

            // ActorClass is sometimes saved as FPackageIndex
            ActorClass = Ar.ReadFName();
            Transform = new FTransform(Ar);
            bSpawnedActor = Ar.ReadBoolean();

            // skip 1 byte then do FPropertyTag, i've tried but keeps causing odd issues.
            ActorData = Ar.ReadArray<byte>();
        }
    }

}
