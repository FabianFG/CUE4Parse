using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Versions;

namespace CUE4Parse.UE4.Objects.PhysicsEngine
{
    public class URB_BodySetup : Assets.Exports.UObject
    {
        public override void Deserialize(FAssetArchive Ar, long validPos)
        {
            base.Deserialize(Ar, validPos);

            if (Ar.Ver >= EUnrealEngineObjectUE3Version.PRECACHE_STATICMESH_COLLISION && Ar.Game < EGame.GAME_UE4_0)
            {
                Ar.ReadArray(() => Ar.ReadArray(() => Ar.ReadBulkArray<byte>())); // PreCachedPhysData
            }
        }
    }
}
