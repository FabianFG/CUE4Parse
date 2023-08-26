using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Objects.UObject;
using Newtonsoft.Json;

namespace CUE4Parse.UE4.Assets.Objects.Properties
{
    [JsonConverter(typeof(DelegatePropertyConverter))]
    public class DelegateProperty : FPropertyTagType<FName>
    {
        public readonly int Num;

        public DelegateProperty(FAssetArchive Ar, ReadType type)
        {
            if (type == ReadType.ZERO)
            {
                Num = 0;
                Value = new FName();
            }
            else
            {
                Num = Ar.Read<int>();
                Value = Ar.ReadFName();
            }
        }
    }
}
