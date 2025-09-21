using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Objects.UObject;

namespace CUE4Parse.UE4.Objects.Engine;

public class FDataCacheDuplicatedObjectData : IUStruct
{
    public string ObjectClassPath;
    public string ObjectOuterPath;
    public FName ObjectName;
    public uint ObjectPersistentFlag;
    public byte[] ObjectData;

    public FDataCacheDuplicatedObjectData(FAssetArchive Ar)
    {
        var version = Ar.Read<byte>();
        ObjectClassPath = Ar.ReadFString();
        ObjectOuterPath = Ar.ReadFString();
        ObjectName = Ar.ReadFName();
        ObjectPersistentFlag = Ar.Read<uint>();
        ObjectData = Ar.ReadArray<byte>();
    }
}
