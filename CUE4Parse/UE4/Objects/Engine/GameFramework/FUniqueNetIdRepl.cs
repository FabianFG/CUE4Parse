using CUE4Parse.UE4.Readers;
using CUE4Parse.UE4.Writers;
using Newtonsoft.Json;

namespace CUE4Parse.UE4.Objects.Engine.GameFramework;

[JsonConverter(typeof(FUniqueNetIdReplConverter))]
public class FUniqueNetIdRepl : IUStruct, ISerializable
{
    public readonly FUniqueNetId? UniqueNetId;
    private readonly int Size;

    public FUniqueNetIdRepl(FArchive Ar)
    {
        Size = Ar.Read<int>();
        if (Size > 0)
        {
            var type = Ar.ReadFName();
            var contents = Ar.ReadString();
            UniqueNetId = new FUniqueNetId(type, contents);
        }
        else
        {
            UniqueNetId = null;
        }
    }

    public void Serialize(FArchiveWriter Ar)
    {
        Ar.Write(Size);

        if (Size > 0 && UniqueNetId != null)
        {
            Ar.Serialize(UniqueNetId);
        }
    }
}