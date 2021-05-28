using CUE4Parse.UE4.Assets.Readers;

namespace CUE4Parse.UE4.Objects.Engine
{
    public class UModel : Assets.Exports.UObject
    {
        public override void Deserialize(FAssetArchive Ar, long validPos)
        {
            base.Deserialize(Ar, validPos);
            Ar.Position = validPos; // TODO read it's contents, this is just to suppress warnings
        }
    }
}