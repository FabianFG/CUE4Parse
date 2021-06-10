using CUE4Parse.UE4.Assets.Exports;
using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Objects.UObject;

namespace CUE4Parse.UE4.Objects.Engine
{
    public class UUserDefinedStruct : UStruct
    {
        public override void Deserialize(FAssetArchive Ar, long validPos)
        {
            base.Deserialize(Ar, validPos);
            if (Flags.HasFlag(EObjectFlags.RF_ClassDefaultObject))
            {
                return;
            }
            // TODO read the default properties
        }
    }
}