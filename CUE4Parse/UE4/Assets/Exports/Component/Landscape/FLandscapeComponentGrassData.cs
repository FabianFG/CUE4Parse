using System;
using System.Collections.Generic;
using CUE4Parse.UE4.Assets.Objects;
using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Objects.UObject;
using CUE4Parse.UE4.Readers;
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
            if (Ar.Game < EGame.GAME_UE4_13 && Ar.Ver >= EUnrealEngineObjectUE4Version.SERIALIZE_LANDSCAPE_GRASS_DATA_MATERIAL_GUID)
            {
                Ar.Position +=16; // Guid
            }

            if (Ar.Game == EGame.GAME_PlayerUnknownsBattlegrounds)
            {
                var bulkData = new FByteBulkData(Ar);
                var data = bulkData.Data ?? [];
                using var tempAr = new FByteArchive("GrassData", data, Ar.Versions);
                HeightData = tempAr.ReadArray<ushort>(data.Length >> 1);
                WeightData = Ar.ReadMap(() => new FPackageIndex(Ar), Array.Empty<byte>);
                return;
            }
            
            HeightData = Ar.ReadBulkArray<ushort>();
            WeightData = Ar.ReadMap(() => new FPackageIndex(Ar), Ar.ReadArray<byte>);
        }
    }
}
