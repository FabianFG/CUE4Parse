using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Exceptions;

namespace CUE4Parse.UE4.Assets.Exports.Chaos;


public interface ISerializationFactory
{
    public ISerializationFactory Serialize(FChaosArchive Ar);

    public ISerializationFactory SerializationFactory(FChaosArchive Ar);
}

public class FChaosArchiveContext
{
    public List<object?> TagToObject = new List<object?>();
    public Dictionary<object, int> ObjToTag  = new Dictionary<object, int>();
    // TSet<void*> PendingAdds;
    public int TagCount;
}

public class FChaosArchive: FAssetArchive
{
    private FChaosArchiveContext Context;

    // public FChaosArchive(string name, Stream baseStream, VersionContainer? versions = null) : base(name, baseStream,
    //     versions) {
    //     Context = new FChaosArchiveContext();
    // }

    public FChaosArchive(FAssetArchive Ar) : base(Ar, Ar.Owner)
    {
        Context = new FChaosArchiveContext();
    }

    public T[] SerializePtrArray<T>(Func<T> getter) where T : ISerializationFactory
    {
        int count = Read<int>();
        if (count < 0)
        {
            throw new ParserException("Invalid count");
        }

        var result = new T[count];
        for (int i = 0; i < count; i++)
        {
            result[i] = SerializePtr(getter());
        }

        return result;
    }

    // can be refactored!
    public T SerializePtr<T>(T obj) where T : ISerializationFactory
    {
        var bExists = ReadBoolean();

        if (!bExists)
        {
            return default!;
        }

        var tag = Read<int>();

        if (tag < 0)
        {
            // error
            throw new ParserException("Invalid tag");
            return default!;
        }

        var slotsNeeded = tag + 1 - Context.TagToObject.Count;
        if (slotsNeeded > 0)
        {
            Context.TagToObject.Add(null);
        }

        // if (!Context->TagToObject.IsValidIndex(Tag))
        // {
        //     InnerArchive.SetCriticalError();
        //     return;
        // }

        // tag
        if (Context.TagToObject.Count < tag)
        {
            throw new ParserException("Invalid tag");
        }

        if (Context.TagToObject[tag] != null)
        {
            return (T) Context.TagToObject[tag]!;
        }
        else {
            var data = obj;
            data = (T) StaticSerialize(data);
            Context.TagToObject[tag] = data;
            return data;
        }
    }

    public ISerializationFactory StaticSerialize(ISerializationFactory obj)
    {
        var createdObj = obj.SerializationFactory(this);
        createdObj.Serialize(this);
        return createdObj;
    }
}
