using CUE4Parse.UE4.Assets.Exports;
using CUE4Parse.UE4.Assets.Readers;

namespace CUE4Parse.FN.Exports
{
    public class ULevelSaveRecord : UObject
    {
        public override void Deserialize(FAssetArchive Ar, long validPos)
        {
            Ar.Position = validPos; // Don't deserialize this, just ignore it silently
        }
    }
}
