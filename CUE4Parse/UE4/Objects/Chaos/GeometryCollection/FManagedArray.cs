using CUE4Parse.UE4.Exceptions;
using CUE4Parse.UE4.Readers;
using CUE4Parse.UE4.Versions;

namespace CUE4Parse.UE4.Objects.Chaos.GeometryCollection;

public class FManagedArray<T>(Func<T> func) : FManagedArrayBase
{
    public T[] Array = [];

    public override void Serialize(FArchive Ar, bool alwaysBulkSerialized)
    {
        var version = Ar.Read<int>();
        if (version > 1) throw new ParserException($"FManagedArray Serialization Version ({version}) > 1");

        if (FDestructionObjectVersion.Get(Ar) >= FDestructionObjectVersion.Type.BulkSerializeArrays || alwaysBulkSerialized)
        {
            Array = Ar.ReadBulkArray(func);
        }
        else
        {
            Array = Ar.ReadArray(func);
        }
    }
}
