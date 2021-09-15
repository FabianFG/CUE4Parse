using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Objects.Core.Math;

namespace CUE4Parse.UE4.Objects.Engine.Level
{
    public readonly struct FPrecomputedVolumeDistanceField : IUStruct
    {
        public readonly float VolumeMaxDistance;
        public readonly FBox VolumeBox;
        public readonly int VolumeSizeX;
        public readonly int VolumeSizeY;
        public readonly int VolumeSizeZ;
        public readonly FColor[] Data;

        public FPrecomputedVolumeDistanceField(FAssetArchive Ar)
        {
            VolumeMaxDistance = Ar.Read<float>();
            VolumeBox = Ar.Read<FBox>();
            VolumeSizeX = Ar.Read<int>();
            VolumeSizeY = Ar.Read<int>();
            VolumeSizeZ = Ar.Read<int>();
            Data = Ar.ReadArray<FColor>();
        }
    }
}