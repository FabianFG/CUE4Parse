using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Objects.Core.i18N;

namespace CUE4Parse.UE4.Assets.Objects
{
    public class TextProperty : FPropertyTagType<FText>
    {
        public TextProperty(FAssetArchive Ar, ReadType type)
        {
            Value = type switch
            {
                ReadType.ZERO => new FText(0, ETextHistoryType.None, new FTextHistory.None()),
                _ => new FText(Ar)
            };
        }
    }
}