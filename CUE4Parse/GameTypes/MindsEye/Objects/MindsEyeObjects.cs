using CUE4Parse.UE4;
using CUE4Parse.UE4.Assets.Readers;

namespace CUE4Parse.GameTypes.MindsEye.Objects;

public class FUgcData : IUStruct
{
    public string Name;
    public string[] Tags;
    public FJsonObjectWrapper SharedMetaData;
    public FJsonObjectWrapper MetaData;
    public int MinPlayerCount;
    public int MaxPlayerCount;

    public FUgcData(FAssetArchive Ar)
    {
        Name = Ar.ReadFString();
        Tags = Ar.ReadArray(Ar.ReadFString);
        SharedMetaData = new FJsonObjectWrapper(Ar);
        MetaData = new FJsonObjectWrapper(Ar);
        MinPlayerCount = Ar.Read<int>();
        MaxPlayerCount = Ar.Read<int>();
    }
}

public class FJsonObjectWrapper(FAssetArchive Ar) : IUStruct
{
    public string JsonString = Ar.ReadFString();
}

public class FUGCPropertyDefaultValueOverride(FAssetArchive Ar) : IUStruct
{
    public byte[] EnumBytes = Ar.ReadBytes(2);
}
