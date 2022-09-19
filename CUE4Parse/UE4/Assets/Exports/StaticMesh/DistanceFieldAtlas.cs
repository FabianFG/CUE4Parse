using CUE4Parse.UE4.Assets.Objects;
using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Objects.Core.Math;
using CUE4Parse.UE4.Readers;
using CUE4Parse.UE4.Versions;

namespace CUE4Parse.UE4.Assets.Exports.StaticMesh
{
    public static class DistanceField
    {
        public const int NumMips = 3;
    }

    public class FSparseDistanceFieldMip
    {
        public FIntVector IndirectionDimensions;
        public int NumDistanceFieldBricks;
        public FVector VolumeToVirtualUVScale;
        public FVector VolumeToVirtualUVAdd;
        public FVector2D DistanceFieldToVolumeScaleBias;
        public uint BulkOffset;
        public uint BulkSize;

        public FSparseDistanceFieldMip(FArchive Ar)
        {
            IndirectionDimensions = Ar.Read<FIntVector>();
            NumDistanceFieldBricks = Ar.Read<int>();
            VolumeToVirtualUVScale = new FVector(Ar);
            VolumeToVirtualUVAdd = new FVector(Ar);
            DistanceFieldToVolumeScaleBias = new FVector2D(Ar);
            BulkOffset = Ar.Read<uint>();
            BulkSize = Ar.Read<uint>();
        }
    }

    public class FDistanceFieldVolumeData
    {
        public ushort[] DistanceFieldVolume; // LegacyIndices
        public FIntVector Size;
        public FBox LocalBoundingBox;
        public bool bMeshWasClosed;
        public bool bBuiltAsIfTwoSided;
        public bool bMeshWasPlane;

        public byte[] CompressedDistanceFieldVolume;
        public FVector2D DistanceMinMax;

        public FDistanceFieldVolumeData(FArchive Ar)
        {
            if (Ar.Game >= EGame.GAME_UE4_16)
            {
                CompressedDistanceFieldVolume = Ar.ReadArray<byte>();
                Size = Ar.Read<FIntVector>();
                LocalBoundingBox = Ar.Read<FBox>();
                DistanceMinMax = Ar.Read<FVector2D>();
                bMeshWasClosed = Ar.ReadBoolean();
                bBuiltAsIfTwoSided = Ar.ReadBoolean();
                bMeshWasPlane = Ar.ReadBoolean();
                DistanceFieldVolume = new ushort[0];
            }
            else
            {
                DistanceFieldVolume = Ar.ReadArray<ushort>();
                Size = Ar.Read<FIntVector>();
                LocalBoundingBox = Ar.Read<FBox>();
                bMeshWasClosed = Ar.ReadBoolean();
                bBuiltAsIfTwoSided = Ar.Ver >= EUnrealEngineObjectUE4Version.RENAME_CROUCHMOVESCHARACTERDOWN && Ar.ReadBoolean();
                bMeshWasPlane = Ar.Ver >= EUnrealEngineObjectUE4Version.DEPRECATE_UMG_STYLE_ASSETS && Ar.ReadBoolean();
                CompressedDistanceFieldVolume = new byte[0];
                DistanceMinMax = new FVector2D(0f, 0f);
            }
        }
    }

    public class FDistanceFieldVolumeData5
    {
        /** Local space bounding box of the distance field volume. */
        public FBox LocalSpaceMeshBounds;

        /** Whether most of the triangles in the mesh used a two-sided material. */
        public bool bMostlyTwoSided;

        public FSparseDistanceFieldMip[] Mips;

        // Lowest resolution mip is always loaded so we always have something
        public byte[] AlwaysLoadedMip;

        // Remaining mips are streamed
        public FByteBulkData StreamableMips;

        public FDistanceFieldVolumeData5(FAssetArchive Ar)
        {
            LocalSpaceMeshBounds = new FBox(Ar);
            bMostlyTwoSided = Ar.ReadBoolean();
            Mips = Ar.ReadArray(DistanceField.NumMips, () => new FSparseDistanceFieldMip(Ar));
            AlwaysLoadedMip = Ar.ReadArray<byte>();
            StreamableMips = new FByteBulkData(Ar);
        }
    }
}
