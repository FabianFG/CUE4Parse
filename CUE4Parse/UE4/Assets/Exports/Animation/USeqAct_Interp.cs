using System.Collections.Generic;
using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Objects.Core.Math;
using CUE4Parse.UE4.Objects.UObject;
using CUE4Parse.UE4.Versions;

namespace CUE4Parse.UE4.Assets.Exports.Animation
{
    public class FSavedTransform
    {
        public FVector Location;
        public FRotator Rotation;

        public FSavedTransform(FAssetArchive Ar)
        {
            Location = Ar.Read<FVector>();
            Rotation = Ar.Read<FRotator>();
        }
    }

    public class USeqAct_Interp : UObject
    {
        public Dictionary<FPackageIndex, FSavedTransform>? SavedActorTransforms;

        public override void Deserialize(FAssetArchive Ar, long validPos)
        {
            base.Deserialize(Ar, validPos);

            if (Ar.Ver >= EUnrealEngineObjectUE3Version.ADDED_SEQACT_INTERP_SAVEACTORTRANSFORMS)
            {
                SavedActorTransforms = Ar.ReadMap(() => new FPackageIndex(Ar), () => new FSavedTransform(Ar));
            }
        }
    }
}
