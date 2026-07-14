using CUE4Parse.UE4.Assets.Objects;
using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Versions;

namespace CUE4Parse.UE4.Assets.Exports.CustomizableObject;

public class FModelStreamableBulkData
{
    public Dictionary<uint, FMutableStreamableBlock> ModelStreamables;
    public Dictionary<uint, FClothingStreamable> ClothingStreamables;
    public Dictionary<uint, FRealTimeMorphStreamable> RealTimeMorphStreamables;
    public FByteBulkData[] StreamableBulkData;

    public FModelStreamableBulkData(FAssetArchive Ar)
    {
        ModelStreamables = Ar.ReadMap(Ar.Read<uint>, () => new FMutableStreamableBlock(Ar));
        if (Ar.Game < GAME_UE5_8)
        {
            ClothingStreamables = Ar.ReadMap(Ar.Read<uint>, () => new FClothingStreamable(Ar));
            RealTimeMorphStreamables = Ar.ReadMap(Ar.Read<uint>, () => new FRealTimeMorphStreamable(Ar));
        }
        StreamableBulkData = Ar.ReadArray(() => new FByteBulkData(Ar));
    }
}
