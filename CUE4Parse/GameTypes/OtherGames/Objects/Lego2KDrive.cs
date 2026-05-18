using CUE4Parse.UE4;
using CUE4Parse.UE4.Assets.Exports;
using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Objects.Core.Math;
using CUE4Parse.UE4.Objects.Engine;
using CUE4Parse.UE4.Objects.UObject;

namespace CUE4Parse.GameTypes.OtherGames.Objects;

public class FPerPlatformSoftObject : TPerPlatformProperty<FSoftObjectPath>
{
    public FPerPlatformSoftObject() { }
    public FPerPlatformSoftObject(FAssetArchive Ar) { Default = new FSoftObjectPath(Ar); }
}

public class FPerPlatformUObject : TPerPlatformProperty<FPackageIndex>
{
    public FPerPlatformUObject() { }
    public FPerPlatformUObject(FAssetArchive Ar) { Default = new FPackageIndex(Ar); }
}

public class FLegoPartLODGeometry : IUStruct
{
    public bool bUsingFallback;
    public bool bHasUVs;
    public bool bGeneratedTangents;
    public FVector[] Positions;
    public FVector4[] Tangents;
    public FVector2D[] UVs;
    public ushort[] Indices;

    public FLegoPartLODGeometry(FAssetArchive Ar)
    {
        bUsingFallback = Ar.ReadBoolean();
        bHasUVs = Ar.ReadBoolean();
        bGeneratedTangents = Ar.ReadBoolean();
        Positions = Ar.ReadBulkArray<FVector>();
        Tangents = Ar.ReadBulkArray<FVector4>();
        UVs = bHasUVs ? Ar.ReadBulkArray<FVector2D>() : [];
        Indices = Ar.ReadBulkArray<ushort>();
    }
}

public class ULegoPartGeometry : UObject
{
    public byte[] Buffer;
    public FIntVector Size;
    public FBox Box;
    public FVector2D SomeVector;
    public FIntVector SomeSize;

    public override void Deserialize(FAssetArchive Ar, long validPos)
    {
        base.Deserialize(Ar, validPos);
        Buffer = Ar.ReadBulkArray<byte>();
        Size = Ar.Read<FIntVector>();
        Box = new FBox(Ar);
        SomeVector = Ar.Read<FVector2D>();
        SomeSize = Ar.Read<FIntVector>();
    }
}
