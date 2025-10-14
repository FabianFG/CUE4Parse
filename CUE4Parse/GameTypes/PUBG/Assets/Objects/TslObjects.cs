using System.Collections.Generic;
using System.Runtime.InteropServices;
using CUE4Parse.UE4;
using CUE4Parse.UE4.Assets.Readers;

namespace CUE4Parse.GameTypes.PUBG.Assets.Objects;

public class FTslSomeSKStruct : IUStruct
{
    public Dictionary<FTslSomeBoneStruct, FTslSomeBoneStruct>[][] SomeBoneStructs;

    public FTslSomeSKStruct(FAssetArchive Ar)
    {
        SomeBoneStructs = Ar.ReadArray(() =>
            Ar.ReadArray(() => Ar.ReadMap(Ar.Read<FTslSomeBoneStruct>, Ar.Read<FTslSomeBoneStruct>)));
    }
}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct FTslSomeBoneStruct
{
    public byte Type;
    public int Bone;
}
