using System.Collections.Generic;
using CUE4Parse.UE4;
using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Objects.Core.Math;
using CUE4Parse.UE4.Objects.UObject;

namespace CUE4Parse.GameTypes.DuneAwakening.Assets.Objects;

public struct FBotAutoBorderCrossingConfig(FAssetArchive Ar) : IUStruct
{
    public bool m_bEnabled = Ar.ReadFlag();
    public float m_DetectBorderDistance = Ar.Read<float>();
    public float m_JumpOverBorderDistance = Ar.Read<float>();
}

public struct FGenericTeamId(FAssetArchive Ar) : IUStruct
{
    public byte TeamId = Ar.Read<byte>();
}
public struct FUniqueID(FAssetArchive Ar) : IUStruct
{
    public long UID = Ar.Read<long>();
}

public enum ECollisionResponse : byte
{
    Ignore = 0,
    Overlap = 1,
    Block = 2,
}

public struct FBodyInstance : IUStruct
{
    public FName CollisionEnabled;
    public Dictionary<FName, ECollisionResponse> ResponseArray;
    public ulong Flags;
    public FVector SomeVector;
    public FVector Scale;

    public FBodyInstance(FAssetArchive Ar)
    {
        CollisionEnabled = Ar.ReadFName();
        var count = Ar.Read<int>();
        var names = Ar.ReadArray(count, Ar.ReadFName);
        var values = Ar.ReadBytes(count);
        ResponseArray = new Dictionary<FName, ECollisionResponse>(count);
        for (var i = 0; i < count; i++)
        {
            ResponseArray[names[i]] = (ECollisionResponse) values[i];
        }
        Flags = Ar.Read<ulong>();

        SomeVector = Ar.Read<FVector>();
        Ar.Position += 96; // some floats/vectors and maybe some enum array at the end
        Scale = new FVector(Ar);
    }
}
