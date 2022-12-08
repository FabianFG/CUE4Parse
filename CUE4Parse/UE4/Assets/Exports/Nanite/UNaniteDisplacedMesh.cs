using CUE4Parse.UE4.Assets.Readers;

namespace CUE4Parse.UE4.Assets.Exports.Nanite
{
    public class UNaniteDisplacedMesh : UObject
    {
        public FNaniteData Data { get; private set; }

        public override void Deserialize(FAssetArchive Ar, long validPos)
        {
            base.Deserialize(Ar, validPos);
            Data = new FNaniteData(Ar);
        }
    }
}
