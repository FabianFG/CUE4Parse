using CUE4Parse.UE4.Assets.Objects;
using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Objects.UObject;

namespace CUE4Parse.UE4.Objects.PCG;

public class FPCGSourceDataContainer : IUStruct
{
    public uint DirtyGeneration;
    public Dictionary<FPCGSourceDataStorageKey, FPCGSourceDataStorageValue> DataStorage;

    public FPCGSourceDataContainer(FAssetArchive Ar)
    {
        DirtyGeneration = Ar.Read<uint>();
        DataStorage = Ar.ReadMap(() => new FPCGSourceDataStorageKey(Ar), () => new FPCGSourceDataStorageValue(Ar));
    }
}

public struct FPCGSourceDataStorageKey(FAssetArchive Ar)
{
    public FName Label = Ar.ReadFName();
    public ulong Hash = Ar.Read<ulong>();
}

public struct FPCGSourceDataStorageValue
{
    public FScriptStruct? Data;

    public FPCGSourceDataStorageValue(FAssetArchive Ar)
    {
        Data = FScriptStruct.ReadInstancedStruct(Ar);
    }
}
