using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Exceptions;
using Newtonsoft.Json;

namespace CUE4Parse.UE4.Assets.Objects.Properties
{
    [JsonConverter(typeof(MapPropertyConverter))]
    public class MapProperty : FPropertyTagType<UScriptMap>
    {
        public MapProperty(FAssetArchive Ar, FPropertyTagData? tagData, ReadType type)
        {
            if (type == ReadType.ZERO)
            {
                Value = new UScriptMap();
            }
            else
            {
                if (tagData == null)
                    throw new ParserException(Ar, "Can't load MapProperty without tag data");
                Value = new UScriptMap(Ar, tagData);
            }
        }
    }
}
