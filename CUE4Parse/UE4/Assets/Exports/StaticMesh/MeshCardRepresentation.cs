using CUE4Parse.UE4.Objects.Core.Math;
using CUE4Parse.UE4.Readers;
using CUE4Parse.UE4.Versions;

namespace CUE4Parse.UE4.Assets.Exports.StaticMesh
{
    public class FLumenCardBuildData
    {
        public FLumenCardOBB OBB;
        public byte LODLevel;
        public byte AxisAlignedDirectionIndex;

        public FLumenCardBuildData(FArchive Ar)
        {
            OBB = Ar.Read<FLumenCardOBB>();
            LODLevel = Ar.Game < EGame.GAME_UE5_1 ? Ar.Read<byte>() : (byte) 0x00;
            AxisAlignedDirectionIndex = Ar.Read<byte>();
        }
    }

    public struct FLumenCardOBB
    {
        public FVector AxisX, AxisY, AxisZ, Origin, Extent;
    }

    public class FCardRepresentationData
    {
        public FBox Bounds;
        public int MaxLodLevel;
        public bool bMostlyTwoSided;
        public FLumenCardBuildData[] CardBuildData;

        public FCardRepresentationData(FArchive Ar)
        {
            Bounds = new FBox(Ar);
            MaxLodLevel = Ar.Game < EGame.GAME_UE5_1 ? Ar.Read<int>() : 0;
            bMostlyTwoSided = Ar.Game >= EGame.GAME_UE5_2 && Ar.ReadBoolean();
            CardBuildData = Ar.ReadArray(() => new FLumenCardBuildData(Ar));
        }
    }
}
