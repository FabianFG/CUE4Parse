using System.Collections;
using CUE4Parse.UE4;
using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.Utils;

namespace CUE4Parse.GameTypes.SOD2.Assets.Objects;

public struct FItemsBitArray(FAssetArchive Ar) : IUStruct
{
    public BitArray? Data = new BitArray(Ar.ReadArray<byte>(((int)Ar.Read<ushort>()).Align(8) >> 3));
}
