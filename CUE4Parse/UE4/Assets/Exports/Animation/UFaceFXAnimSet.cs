using CUE4Parse.UE4.Assets.Readers;

namespace CUE4Parse.UE4.Assets.Exports.Animation
{
    public abstract class UFaceFXAnimSet : UObject
    {
        public override void Deserialize(FAssetArchive Ar, long validPos)
        {
            base.Deserialize(Ar, validPos);

            Ar.ReadArray(() => Ar.ReadArray<byte>()); // RawFaceFXAnimSetBytes
            Ar.ReadArray(() => Ar.ReadArray<byte>()); // RawFaceFXMiniSessionBytes
        }
    }
}
