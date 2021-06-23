using CUE4Parse.UE4.Objects.Core.Math;
using CUE4Parse.UE4.Readers;
using CUE4Parse.UE4.Versions;

namespace CUE4Parse.UE4.Assets.Exports.StaticMesh
{
    public class FDistanceFieldVolumeData
    {
        public ushort[] DistanceFieldVolume; // LegacyIndices
        public FIntVector Size;
        public FBox LocalBoundingBox;
        public bool MeshWasClosed;
        public bool BuiltAsIfTwoSided;
        public bool MeshWasPlane;

        public int[] CompressedDistanceFieldVolume;
        public FVector2D DistanceMinMax;

        public FDistanceFieldVolumeData(FArchive Ar)
        {
            if (Ar.Game >= EGame.GAME_UE4_16)
            {
                CompressedDistanceFieldVolume = Ar.ReadArray(() => Ar.ReadByte());
                Size = Ar.Read<FIntVector>();
                LocalBoundingBox = Ar.Read<FBox>();
                DistanceMinMax = Ar.Read<FVector2D>();
                MeshWasClosed = Ar.ReadBoolean();
                BuiltAsIfTwoSided = Ar.ReadBoolean();
                MeshWasPlane = Ar.ReadBoolean();
                DistanceFieldVolume = new ushort[0];
            }
            else
            {
                DistanceFieldVolume = Ar.ReadArray<ushort>();
                Size = Ar.Read<FIntVector>();
                LocalBoundingBox = Ar.Read<FBox>();
                MeshWasClosed = Ar.ReadBoolean();
                BuiltAsIfTwoSided = Ar.Ver >= UE4Version.VER_UE4_RENAME_CROUCHMOVESCHARACTERDOWN ? Ar.ReadBoolean() : false;
                MeshWasPlane = Ar.Ver >= UE4Version.VER_UE4_DEPRECATE_UMG_STYLE_ASSETS ? Ar.ReadBoolean() : false;
                CompressedDistanceFieldVolume = new int[0];
                DistanceMinMax = new FVector2D(0f, 0f);
            }
        }
    }
}
