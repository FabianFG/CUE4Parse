using CUE4Parse.UE4;
using CUE4Parse.UE4.Assets.Objects;
using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Objects.UObject;

namespace CUE4Parse.GameTypes.OuterWorlds2.Objects;

public class FAIBehaviorTreeReference : IUStruct
{
    public byte Type;
    public FSoftObjectPath BehaviorTreeClass;
    public FAISubBehaviorTreeInstance[] SubBehaviorTreeInstances;
    public string SubTreeInstanceCommonPackageName;
    public string SubTreeInstanceCommonOuterPath;

    public FAIBehaviorTreeReference(FAssetArchive Ar)
    {
        Type = Ar.Read<byte>();
        BehaviorTreeClass = new FSoftObjectPath(Ar);
        var subTreeCount = Ar.Read<int>();
        SubTreeInstanceCommonPackageName = Ar.ReadFString();
        SubTreeInstanceCommonOuterPath = Ar.ReadFString();
        SubBehaviorTreeInstances = Ar.ReadArray(subTreeCount , () => new FAISubBehaviorTreeInstance(Ar, Type));
    }
}

public class FAISubBehaviorTreeInstance
{
    public FName ObjectName;
    public uint ObjectPersistentFlags;
    public FStructFallback PropertyDataWithObjects;

    public FAISubBehaviorTreeInstance(FAssetArchive Ar, byte type)
    {
        ObjectName = Ar.ReadFName();
        ObjectPersistentFlags = Ar.Read<uint>();
        PropertyDataWithObjects = new FPropertyDataWithObjects(Ar, type);
    }
}
