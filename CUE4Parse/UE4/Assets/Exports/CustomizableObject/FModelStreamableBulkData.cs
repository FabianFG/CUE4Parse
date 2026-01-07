using System.Collections.Generic;
using System.Runtime.CompilerServices;
using CUE4Parse.UE4.Assets.Objects;
using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Readers;

namespace CUE4Parse.UE4.Assets.Exports.CustomizableObject;

public class FModelStreamableBulkData
{
    public Dictionary<uint, FMutableStreamableBlock> ModelStreamables;
    public Dictionary<uint, FClothingStreamable> ClothingStreamables;
    public Dictionary<uint, FRealTimeMorphStreamable> RealTimeMorphStreamables;
    public FByteBulkData[] StreamableBulkData;
    
    public FModelStreamableBulkData(FAssetArchive Ar)
    {
        ModelStreamables = Ar.ReadMap(Ar.Read<uint>, Ar.Read<FMutableStreamableBlock>);
        ClothingStreamables = Ar.ReadMap(Ar.Read<uint>, Ar.Read<FClothingStreamable>);
        RealTimeMorphStreamables = Ar.ReadMap(Ar.Read<uint>, () => new FRealTimeMorphStreamable(Ar));
        StreamableBulkData = Ar.ReadArray(() => new FByteBulkData(Ar));
    }
}