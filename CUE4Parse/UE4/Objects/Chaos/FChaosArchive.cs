using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Exceptions;

namespace CUE4Parse.UE4.Objects.Chaos;

public class FChaosArchive(FAssetArchive ar) : FAssetArchive(ar, ar.Owner)
{
    private readonly List<object?> _tagToObject = [];

    public T?[] ReadPtrArray<T>() where T : IChaosClass
    {
        var count = Read<int>();
        if (count < 0) throw new ParserException("Invalid ChaosClass array count");

        var result = new T?[count];
        for (var i = 0; i < result.Length; i++)
        {
            result[i] = ReadPtr<T>();
        }

        return result;
    }

    public T? ReadPtr<T>() where T : IChaosClass
    {
        var bExists = ReadBoolean();
        if (!bExists) return default;

        var tag = Read<int>();
        if (tag < 0) throw new ParserException("FChaosArchive Tag < 0");

        var slotsNeeded = tag + 1 - _tagToObject.Count;
        if (slotsNeeded > 0) _tagToObject.Add(null);

        if (tag >= _tagToObject.Count) throw new ParserException("Tag >= TagToObject.Count");

        if (_tagToObject[tag] != null)
        {
            var obj = _tagToObject[tag];
            return obj is not T expectedReturnType ? throw new InvalidOperationException( $"Chaos object with tag {tag} is of type {obj?.GetType().Name}, expected {typeof(T).Name}.") : expectedReturnType;
        }
        else
        {
            var obj = StaticSerialize<T>();
            if (obj is not T expectedReturnType)
                throw new InvalidOperationException($"Chaos object with tag {tag} is of type {obj.GetType().Name}, expected {typeof(T).Name}.");

            _tagToObject[tag] = expectedReturnType;
            return expectedReturnType;
        }
    }

    private IChaosClass StaticSerialize<T>() where T : IChaosClass
    {
        var obj = T.SerializationFactory(this);
        obj.Serialize(this);

        return obj;
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);

        _tagToObject.Clear();
    }
}
