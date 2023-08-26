using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Objects.UObject;
using Newtonsoft.Json;

namespace CUE4Parse.UE4.Assets.Objects.Properties
{
    [JsonConverter(typeof(SoftObjectPropertyConverter))]
    public class SoftObjectProperty : FPropertyTagType<FSoftObjectPath>
    {
        public SoftObjectProperty(FAssetArchive Ar, ReadType type)
        {
            if (type == ReadType.ZERO)
            {
                Value = new FSoftObjectPath();
            }
            else
            {
                //var pos = Ar.Position;
                Value = new FSoftObjectPath(Ar);
                //if (!Ar.HasUnversionedProperties && type == ReadType.MAP && Ar.Game != EGame.GAME_Valorant)
                //{
                //    // skip ahead, putting the total bytes read to 16
                //    Ar.Position += 16 - (Ar.Position - pos);
                //}
            }
        }
    }
}
