using System.Collections.Generic;
using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Objects.UObject;

namespace CUE4Parse.UE4.Assets.Exports.Landscape;

public class FLandscapeComponentGrassData
{
    public int NumElements;
    public Dictionary<FPackageIndex, int> WeightOffsets;
    public byte[] HeightWeightData;

    public FLandscapeComponentGrassData(FAssetArchive Ar)
    {
        NumElements = Ar.Read<int>();
        WeightOffsets = Ar.ReadMap(() => new FPackageIndex(Ar), Ar.Read<int>);
        HeightWeightData = Ar.ReadArray<byte>();
    }
}
