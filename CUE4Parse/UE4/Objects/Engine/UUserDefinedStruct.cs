using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Objects.UObject;

namespace CUE4Parse.UE4.Objects.Engine
{
    public class UUserDefinedStruct : UStruct
    {
        public override void Deserialize(FAssetArchive Ar, long validPos)
        {
            base.Deserialize(Ar, validPos);
            if ((Flags & 0x10) != 0)
            {
                return;
            }
            // TODO read the default properties
        }
    }
}