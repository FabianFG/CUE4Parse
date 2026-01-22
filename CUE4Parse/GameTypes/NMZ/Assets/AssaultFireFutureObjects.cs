using System.Collections.Generic;
using CUE4Parse.UE4;
using CUE4Parse.UE4.Assets.Objects;
using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Objects.Core.Math;
using CUE4Parse.UE4.Objects.UObject;

namespace CUE4Parse.GameTypes.NMZ.Assets;

public class FGPRowName(FAssetArchive Ar) : IUStruct
{
    public FName Name = Ar.ReadFName();
    public FName Id = Ar.ReadFName();
}

public class FAssaultFireFutureCustomStruct(FAssetArchive Ar) : IUStruct
{
    public FSoftObjectPath Unknown1 = new FSoftObjectPath(Ar);
    public FVector Unknown2 = Ar.Read<FVector>();
    public FStructFallback Unknown3 = new FStructFallback(Ar);
    public float Unknown4 = Ar.Read<float>();
}

public class FAnnotationPointData2 : IUStruct
{
    public byte Type;
    public FVector SomeVector;
    public IUStruct? SomeStruct;
    public FVector2D SomeVector2D;
    public FStructFallback StructFallback2;

    public FAnnotationPointData2(FAssetArchive Ar)
    {
        Type = Ar.Read<byte>();
        SomeVector = Ar.Read<FVector>();
        switch (Type)
        {
            case 4:
                var type = Ar.Read<byte>();
                SomeStruct = Ar.Read<FVector>();
                break;
            case 5:
                SomeStruct = new FStructFallback(Ar);
                break;
            case 6:
                SomeStruct = new FAssaultFireFutureCustomStruct(Ar);
                break;
            default:
                SomeStruct = null;
                break;
        }
        SomeVector2D = Ar.Read<FVector2D>();
        StructFallback2 = new FStructFallback(Ar);
    }
}

public class FAssetDataSerializable(FAssetArchive Ar) : IUStruct
{
    public readonly FName ObjectPath = Ar.ReadFName();
    public readonly FName PackagePath = Ar.ReadFName();
    public readonly FName AssetClass = Ar.ReadFName();
    public readonly FName PackageName = Ar.ReadFName();
    public readonly FName AssetName = Ar.ReadFName();
    public Dictionary<FName, string> TagsAndValues = Ar.ReadMap(Ar.ReadFName, Ar.ReadFString);
    public readonly int[] ChunkIDs = Ar.ReadArray<int>();
    public readonly EPackageFlags PackageFlags = Ar.Read<EPackageFlags>();
}
