using System.Runtime.InteropServices;
using CUE4Parse.UE4.Objects.Core.Math;
using CUE4Parse.UE4.Readers;

namespace CUE4Parse.UE4.Assets.Exports.StaticMesh
{
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct FLumenCardBuildData
    {
        public FLumenCardOBB OBB;
        public byte LODLevel;
        public byte AxisAlignedDirectionIndex;
    }

    public struct FLumenCardOBB
    {
        public FVector Origin, AxisX, AxisY, AxisZ, Extent;
    }

    public class FCardRepresentationData
    {
        public FBox Bounds;
        public int MaxLodLevel;
        public FLumenCardBuildData[] CardBuildData;

        public FCardRepresentationData(FArchive Ar)
        {
            Bounds = Ar.Read<FBox>();
            MaxLodLevel = Ar.Read<int>();
            CardBuildData = Ar.ReadArray<FLumenCardBuildData>();
        }
    }
}