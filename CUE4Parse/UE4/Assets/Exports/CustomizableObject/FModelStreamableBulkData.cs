using System.Collections.Generic;
using CUE4Parse.UE4.Assets.Objects;
using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Readers;

namespace CUE4Parse.UE4.Assets.Exports.CustomizableObject;

public class FModelStreamableBulkData
{
    public KeyValuePair<uint, FMutableStreamableBlock>[] ModelStreamables;
    public KeyValuePair<uint, FClothingStreamable>[] ClothingStreamables;
    public KeyValuePair<uint, FRealTimeMorphStreamable>[] RealTimeMorphStreamables;
    public FByteBulkData[] HashToBulkData;

    public FModelStreamableBulkData(FAssetArchive Ar, bool bCooked)
    {
        ModelStreamables = Ar.ReadArray(() => new KeyValuePair<uint, FMutableStreamableBlock>(Ar.Read<uint>(), new FMutableStreamableBlock(Ar)));
        ClothingStreamables = Ar.ReadArray(() => new KeyValuePair<uint, FClothingStreamable>(Ar.Read<uint>(), new FClothingStreamable(Ar)));
        RealTimeMorphStreamables = Ar.ReadArray(() => new KeyValuePair<uint, FRealTimeMorphStreamable>(Ar.Read<uint>(), new FRealTimeMorphStreamable(Ar)));

        if (bCooked)
        {
            var numBulkDatas = Ar.Read<int>();
            HashToBulkData = new FByteBulkData[numBulkDatas];

            for (int i = 0; i < HashToBulkData.Length; i++)
            {
                HashToBulkData[i] = new FByteBulkData(Ar);
            }
        }
    }
}