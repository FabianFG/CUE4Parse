using CUE4Parse.UE4.Exceptions;
using CUE4Parse.UE4.Versions;

namespace CUE4Parse.UE4.Objects.Chaos.GeometryCollection;

public class TManagedArrayBase<T> : FManagedArrayBase
{
    public T[] Array;
    private readonly Func<T> _func;
    private readonly bool _bReadAsNormalArray;

    public TManagedArrayBase(Func<T> func, bool bReadAsNormalArray)
    {
        Array = [];
        _func = func;
        _bReadAsNormalArray = bReadAsNormalArray;
    }

    public override void Serialize(FChaosArchive Ar)
    {
        var version = Ar.Read<int>();
        if (version > 1) throw new ParserException($"TManagedArrayBase Serialization Version ({version}) > 1");

        if (FDestructionObjectVersion.Get(Ar) < FDestructionObjectVersion.Type.BulkSerializeArrays || _bReadAsNormalArray)
        {
            Array = Ar.ReadArray(_func);
        }
        else
        {
            Array = Ar.ReadBulkArray(_func);
        }
    }

    public override T1[] GetArray<T1>()
    {
        if (typeof(T1) != typeof(T)) throw new InvalidOperationException($"Invalid type requested: {typeof(T1)}");
        return (T1[]) (object) Array;
    }
}