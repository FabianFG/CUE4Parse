using CUE4Parse.UE4;
using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Objects.Core.Math;
using CUE4Parse.UE4.Objects.UObject;

namespace CUE4Parse.GameTypes.Gothic1R.Assets.Objects;

public struct FWaynetPath(FAssetArchive Ar) : IUStruct
{
    public bool IsValid = Ar.ReadFlag();
    public FVector[] Path = Ar.ReadArray(() => new FVector(Ar));
    public byte[] Flags = Ar.ReadArray<byte>();
    public FVector2D Position = Ar.Read<FVector2D>();
}

public struct FWaynetNode(FAssetArchive Ar) : IUStruct
{
    public bool IsValid = Ar.ReadFlag();
    public ulong Index = Ar.Read<ulong>();
    public FVector Position = new FVector(Ar);
    public bool NotFailed = Ar.ReadBoolean();
    public FName Name = Ar.ReadFName();
    public ulong[] Neighbours = Ar.ReadArray<ulong>();
    public ushort Flags = Ar.Read<ushort>();
}
