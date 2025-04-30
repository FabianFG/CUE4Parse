using System.Collections.Generic;
using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Objects.UObject;
using CUE4Parse.UE4.Versions;

namespace CUE4Parse.UE4.Assets.Exports.Component.Landscape;

public class FLandscapeComponentGrassData
{
    public int NumElements;
    public Dictionary<FPackageIndex, int> WeightOffsets;
    public byte[] HeightWeightData;
    public ushort[] HeightData;
    public Dictionary<FPackageIndex, byte[]> WeightData;

    public FLandscapeComponentGrassData(FAssetArchive Ar)
    {
        if (Ar.Game >= Versions.EGame.GAME_UE5_0)
        {
            NumElements = Ar.Read<int>();
            WeightOffsets = Ar.ReadMap(() => new FPackageIndex(Ar), Ar.Read<int>);
            HeightWeightData = Ar.ReadArray<byte>();
        }
        else
        {
            if (Ar.Game <= EGame.GAME_UE4_12 && Ar.Ver >= EUnrealEngineObjectUE4Version.SERIALIZE_LANDSCAPE_GRASS_DATA_MATERIAL_GUID)
            {
                Ar.Position +=16; // Guid
            }

            HeightData = Ar.ReadBulkArray<ushort>();
            WeightData = Ar.ReadMap(() => new FPackageIndex(Ar), Ar.ReadArray<byte>);
        }
    }
}
