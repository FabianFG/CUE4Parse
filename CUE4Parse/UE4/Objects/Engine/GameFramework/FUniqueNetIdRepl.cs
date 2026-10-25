using CUE4Parse.UE4.Readers;
using Newtonsoft.Json;

namespace CUE4Parse.UE4.Objects.Engine.GameFramework;

[JsonConverter(typeof(FUniqueNetIdReplConverter))]
public class FUniqueNetIdRepl : IUStruct
{
    public readonly FUniqueNetId? UniqueNetId;

    public FUniqueNetIdRepl(FArchive Ar)
    {
        var size = Ar.Read<int>();
        UniqueNetId = size > 0 ? new FUniqueNetId(Ar.ReadFName().Text, Ar.ReadFString()) : null;
    }
}
