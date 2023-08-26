using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Objects.Core.i18N;
using Newtonsoft.Json;

namespace CUE4Parse.UE4.Assets.Objects.Properties
{
    [JsonConverter(typeof(TextPropertyConverter))]
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
