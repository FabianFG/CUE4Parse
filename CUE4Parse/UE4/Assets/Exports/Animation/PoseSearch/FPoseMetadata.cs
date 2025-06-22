using System;
using CUE4Parse.UE4.Assets.Readers;

namespace CUE4Parse.UE4.Assets.Exports.Animation.PoseSearch;

public struct FPoseMetadata(FAssetArchive Ar)
{
    // enum { ValueOffsetNumBits = 27 };
    // enum { AssetIndexNumBits = 20 };
    // enum { BlockTransitionNumBits = 1 };
    public uint ValueOffset = Ar.Read<uint>();
    public uint AssetIndex = Ar.Read<uint>();
    public bool bInBlockTransition = Ar.ReadBoolean();
    public float CostAddend = (float) Ar.Read<Half>();
}
