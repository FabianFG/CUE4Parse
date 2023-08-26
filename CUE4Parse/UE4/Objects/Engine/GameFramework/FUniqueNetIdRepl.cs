using CUE4Parse.UE4.Readers;
using Newtonsoft.Json;

namespace CUE4Parse.UE4.Objects.Engine.GameFramework
{
    [JsonConverter(typeof(FUniqueNetIdReplConverter))]
    public class FUniqueNetIdRepl : IUStruct
    {
        public readonly FUniqueNetId? UniqueNetId;

        public FUniqueNetIdRepl(FArchive Ar)
        {
            var size = Ar.Read<int>();
            if (size > 0)
            {
                var type = Ar.ReadFName();
                var contents = Ar.ReadString();
                UniqueNetId = new FUniqueNetId(type.Text, contents);
            }
            else
            {
                UniqueNetId = null;
            }
        }
    }
}
