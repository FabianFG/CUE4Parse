using System;
using CUE4Parse.UE4.Objects.Core.Math;
using CUE4Parse.UE4.Readers;
using CUE4Parse.UE4.Versions;

namespace CUE4Parse.UE4.Assets.Exports.Animation
{
    public class FRawAnimSequenceTrack
    {
        public readonly FVector[] PosKeys;
        public readonly FQuat[] RotKeys;
        public readonly FVector[] ScaleKeys;

        public FRawAnimSequenceTrack(FArchive Ar)
        {
            PosKeys = Ar.ReadBulkArray<FVector>();
            RotKeys = Ar.ReadBulkArray<FQuat>();
            ScaleKeys = Ar.Ver >= EUnrealEngineObjectUE4Version.ANIM_SUPPORT_NONUNIFORM_SCALE_ANIMATION ? Ar.ReadBulkArray<FVector>() : Array.Empty<FVector>();
        }
    }
}
