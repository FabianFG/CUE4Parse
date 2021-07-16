using CUE4Parse.UE4.Objects.Core.Math;
using CUE4Parse.UE4.Readers;
using CUE4Parse.UE4.Versions;

namespace CUE4Parse.UE4.Assets.Exports.Animation
{
    public class FRawAnimSequenceTrack
    {
        public FVector[] PosKeys;
        public FQuat[] RotKeys;
        public FVector[] ScaleKeys;

        public FRawAnimSequenceTrack(FArchive Ar)
        {
            PosKeys = Ar.ReadBulkArray<FVector>();
            RotKeys = Ar.ReadBulkArray<FQuat>();

            if (Ar.Ver >= UE4Version.VER_UE4_ANIM_SUPPORT_NONUNIFORM_SCALE_ANIMATION)
            {
                ScaleKeys = Ar.ReadBulkArray<FVector>();
            }
        }
    }
}