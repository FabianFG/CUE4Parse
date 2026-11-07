using System.Runtime.InteropServices;
using CUE4Parse.UE4.Assets.Objects;
using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Objects.Core.Math;
using CUE4Parse.UE4.Objects.Engine;
using CUE4Parse.UE4.Objects.RenderCore;
using CUE4Parse.UE4.Readers;
using CUE4Parse.UE4.Versions;

namespace CUE4Parse.UE4.Assets.Exports.HairStrands;

// initial support for UGroomAsset, should work on UE5.2+
public class UGroomAsset : UObject
{
    public const int HAIR_CURVE_ATTRIBUTE_COUNT = 5;
    public const int HAIR_POINT_ATTRIBUTE_COUNT = 3;
    //public FHairGroupPublicResource HairGroupPublicResource;
    //public FHairGroupsMaterial HairGroupsMaterial;
    //public FHairGroupsLOD HairGroupsLOD;
    //public FHairStrandsMaterial HairStrandsMaterial;
    //public FHairStrandsLOD HairStrandsLOD;

    enum EClassDataStripFlag : byte
    {
        CDSF_ImportedStrands = 1,
        CDSF_MinLodData = 2,
        CDSF_StrandsStripped = 4,
        CDSF_CardsStripped = 8,
        CDSF_MeshesStripped = 16
    };

    public FStripDataFlags ClassDataStripFlags;
    public FHairGroupPlatformData[] HairGroupsPlatformData;

    public override void Deserialize(FAssetArchive Ar, long validPos)
    {
        base.Deserialize(Ar, validPos);
#if DEBUG
        Log.Debug(nameof(UGroomAsset));
#endif

        if (FPhysicsObjectVersion.Get(Ar) >= FPhysicsObjectVersion.Type.GroomWithDescription)
        {
            bool bHasImportedStrands = true;
            if (FAnimObjectVersion.Get(Ar) >= FAnimObjectVersion.Type.GroomLODStripping)
            {
                ClassDataStripFlags = new FStripDataFlags(Ar);
                bHasImportedStrands = ClassDataStripFlags.IsClassDataStripped((byte)EClassDataStripFlag.CDSF_ImportedStrands);
            }
            Ar.Position += 1; //idk why maybe some game specific

            if (ClassDataStripFlags.IsEditorDataStripped())
            {
                HairGroupsPlatformData = Ar.ReadArray(() => new FHairGroupPlatformData(Ar));
            }
            else if (FUE5MainStreamObjectVersion.Get(Ar) < FUE5MainStreamObjectVersion.Type.GroomAssetRemoveInAssetSerialization && !bHasImportedStrands)
            {
                HairGroupsPlatformData = Ar.ReadArray(() => new FHairGroupPlatformData(Ar));
            }
        }
    }

    public class FHairGroupPlatformData
    {
        public FGuides Guides;
        public FStrands Strands;
        public FCards? Cards;
        public FMeshes? Meshes;

        public void DummySerizalizationForLegacyPurpose(FAssetArchive Ar)
        {
            Strands = new FStrands();
            Guides = new FGuides(Ar);
        }

        public FHairGroupPlatformData(FAssetArchive Ar)
        {
            if (FUE5MainStreamObjectVersion.Get(Ar) < FUE5MainStreamObjectVersion.Type.GroomAssetRemoveInAssetSerialization)
            {
                DummySerizalizationForLegacyPurpose(Ar);
                return;
            }

            Guides = new FGuides(Ar);
            Strands = new FStrands(Ar);

            if (FAnimObjectVersion.Get(Ar) >= FAnimObjectVersion.Type.SerializeGroomCardsAndMeshes)
            {
                var bIsCooked = Ar.ReadBoolean();
                if (bIsCooked)
                {
                    Cards = new FCards(Ar);
                    Meshes = new FMeshes(Ar);
                }
            }
        }

        public class FGuides
        {
            public FHairStrandsBulkData BulkData;

            public FGuides(FAssetArchive Ar)
            {
                BulkData = new FHairStrandsBulkData();
                BulkData.Serialize(Ar);
            }
        }

        public class FStrands
        {
            public FHairStrandsBulkData BulkData;
            public FHairStrandsInterpolationBulkData InterpolationBulkData;
            public FHairStrandsClusterBulkData? ClusterBulkData;
            // Experimental (Optional)
            // public FRenderCurveResourceData CurveResourceData;

            public FStrands() { }

            public FStrands(FAssetArchive Ar, bool bHeader = true, bool bData = true)
            {
                BulkData = new FHairStrandsBulkData();
                BulkData.Serialize(Ar);
                InterpolationBulkData = new FHairStrandsInterpolationBulkData();
                InterpolationBulkData.Serialize(Ar);
                if (FAnimObjectVersion.Get(Ar) >= FAnimObjectVersion.Type.SerializeHairClusterCullingData)
                {
                    ClusterBulkData = new FHairStrandsClusterBulkData();
                    ClusterBulkData.Serialize(Ar);
                }
                // r.RenderCurve - Enable render curve raster pipeline. Read-only variable. (experimental).,
                //if (IsRenderCurveEnabled())
                //{
                //    if (bHeader){ StrandData.CurveResourceData.Serialize(Ar, Owner); }
                //}
            }
        };

        public class FCards
        {
            public FLOD[] LODs;

            public FCards(FAssetArchive Ar)
            {
                LODs = Ar.ReadArray(() => new FLOD(Ar));
            }

            public class FLOD
            {
                public FHairCardsBulkData BulkData;
                public FHairCardsInterpolationBulkData InterpolationBulkData;
                public FHairStrandsBulkData GuideBulkData;
                public FHairStrandsInterpolationBulkData GuideInterpolationBulkData;

                public FLOD(FAssetArchive Ar)
                {
                    BulkData = new FHairCardsBulkData(Ar);
                    InterpolationBulkData = new FHairCardsInterpolationBulkData(Ar);
                    GuideBulkData = new FHairStrandsBulkData();
                    GuideBulkData.Serialize(Ar);
                    GuideInterpolationBulkData = new FHairStrandsInterpolationBulkData();
                    GuideInterpolationBulkData.Serialize(Ar);
                }
            }
        }

        public class FMeshes(FAssetArchive ar)
        {
            public FLOD[] LODs = ar.ReadArray(() => new FLOD(ar));

            public class FLOD(FAssetArchive ar)
            {
                FHairMeshesBulkData BulkData = new FHairMeshesBulkData(ar);
            }
        }

        public class FHairMeshesBulkData
        {
            public FVector4[] Positions;
            public FPackedNormal[] Normals;
            public FVector4[] UVs;
            public uint[] VertexColors;
            public uint[] Indices;
            public FHeader Header;

            public struct FHeader(FArchive Ar)
            {
                public FBox BoundingBox = new(Ar);
                public uint NumUVs = Ar.Game >= GAME_UE5_7 ? Ar.Read<uint>() : 0;
                public bool bVertexColor = Ar.Game >= GAME_UE5_6 && Ar.ReadBoolean();
            }

            public FHairMeshesBulkData(FAssetArchive Ar)
            {
                Positions = Ar.ReadArray<FVector4>();
                Normals = Ar.ReadArray<FPackedNormal>();// maybe need to xor??
                UVs = Ar.ReadArray<FVector4>();
                VertexColors = Ar.Game >= GAME_UE5_6 ? Ar.ReadArray<uint>() : [];
                Indices = Ar.ReadArray<uint>();;
                Header = new FHeader(Ar);
            }
        }

        public class FHairCardsInterpolationBulkData
        {
            public uint[] Interpolation; //24 bit VertexIndex + 8 bit VertexLerp

            public FHairCardsInterpolationBulkData(FArchive Ar)
            {
                Interpolation = Ar.ReadArray<uint>();
            }
        }

        public class FHairCardsBulkData
        {
            public FVector4[] Positions;
            public FPackedNormal[] Normals;
            public FVector4[] UVs;
            public uint[] Materials;
            public uint[] VertexColors;
            public uint[] Indices;
            public FHeader Header;

            public struct FHeader(FArchive Ar)
            {
                public FBox BoundingBox = new(Ar);
                public uint NumUVs = Ar.Game >= GAME_UE5_7 ? Ar.Read<uint>() : 0;
                public bool bVertexColor = Ar.Game >= GAME_UE5_6 && Ar.ReadBoolean();
            }

            public FHairCardsBulkData(FAssetArchive Ar)
            {
                Positions = Ar.ReadArray<FVector4>();
                Normals = Ar.ReadArray<FPackedNormal>();// maybe need to xor??
                UVs = Ar.ReadArray<FVector4>();
                Materials = Ar.ReadArray<uint>();
                VertexColors = Ar.Game >= GAME_UE5_6 ? Ar.ReadArray<uint>() : [];
                Indices = Ar.ReadArray<uint>();;
                Header = new FHeader(Ar);
            }
        }
    }
}

public class FHairStrandsClusterBulkData : FHairStrandsBulkCommon
{
    public FHeader? Header;
    public FData? Data;

    public class FHeader(FArchive Ar)
    {
        public uint ClusterCount = Ar.Read<uint>();
        public float ClusterScale = Ar.Read<float>();
        public uint PointCount = Ar.Read<uint>();
        public uint CurveCount = Ar.Read<uint>();
        public FVector4 ClusterInfoParameters = Ar.Read<FVector4>(); // xy:Scale/Offset for ScreenSize, zw:Scale/Offset for Radius
        public FHairLODInfo[] LODInfos = Ar.ReadArray(() => new FHairLODInfo((Ar)));
        public FStrides Strides = Ar.Read<FStrides>();

        [StructLayout(LayoutKind.Sequential, Pack = 4)]
        public struct FStrides
        {
            public uint PackedClusterInfoStride;
            public uint CurveToClusterIdStride;
            public uint PointLODStride;
        }
    }

    public class FData
    {
        public FByteBulkData? PackedClusterInfos;
        public FByteBulkData? CurveToClusterIds;
        public FByteBulkData? PointLODs;

        public FData(FAssetArchive Ar)
        {
            PackedClusterInfos = new FByteBulkData(Ar);
            CurveToClusterIds = new FByteBulkData(Ar);
            PointLODs = new FByteBulkData(Ar);
        }
    }

    public struct FHairLODInfo
    {
        public uint CurveCount = 0;
        public uint PointCount = 0;
        public float RadiusScale = 1;
        public float ScreenSize = 1;
        public bool bIsVisible = true;

        public FHairLODInfo(FArchive Ar)
        {
            CurveCount = Ar.Read<uint>();
            PointCount = Ar.Read<uint>();
            RadiusScale = Ar.Read<float>();
            ScreenSize = Ar.Read<float>();
            bIsVisible = Ar.ReadBoolean();
        }
    };

    public override void SerializeHeader(FAssetArchive Ar)
    {
        Header = new FHeader(Ar);
    }

    public override void SerializeData(FAssetArchive Ar)
    {
        Data = new FData(Ar);
    }
}

public class FHairStrandsInterpolationBulkData : FHairStrandsBulkCommon
{
    public FHeader? Header;
    public FData? Data;

    [Flags]
    public enum EDataFlags : uint
    {
        DataFlags_HasData = 0x1u,
        DataFlags_HasSingleGuideData = 0x2,      // Contains point attribute data.
    };

    public class FHeader(FArchive Ar)
    {
        public EDataFlags Flags = Ar.Read<EDataFlags>();
        public uint PointCount = Ar.Read<uint>();
        public uint CurveCount = Ar.Read<uint>();
        public FStrides Strides = Ar.Read<FStrides>();

        [StructLayout(LayoutKind.Sequential, Pack = 4)]
        public struct FStrides
        {
            public uint CurveInterpolationStride;
            public uint PointInterpolationStride;
        }
    }

    public class FData
    {
        public FByteBulkData? CurveInterpolation;
        public FByteBulkData? PointInterpolation;

        public FData(FAssetArchive Ar, ref FHeader header)
        {
            CurveInterpolation = new FByteBulkData(Ar);
            PointInterpolation = new FByteBulkData(Ar);
        }
    }

    public override void SerializeHeader(FAssetArchive Ar)
    {
        Header = new FHeader(Ar);
    }

    public override void SerializeData(FAssetArchive Ar)
    {
        if (Header?.Flags.HasFlag(EDataFlags.DataFlags_HasData) == false)
            return;

        Data = new FData(Ar, ref Header!);
    }
}

public class FHairStrandsBulkData : FHairStrandsBulkCommon
{
    public FHeader? Header;
    public FData? Data;

    [Flags]
    public enum EDataFlags : uint
    {
        DataFlags_HasData = 0x1u,               // Contains valid data. Otherwise: Position, Attributes, ... are all empty
        DataFlags_HasPointAttribute = 0x2,      // Contains point attribute data.
        DataFlags_HasTranscodedPosition = 0x4u, // Contains transcoded position
        DataFlags_HasTrimmedCurve = 0x8u,       // Source data had more curves per group than the limit and some curves were trimmed
        DataFlags_HasTrimmedPoint = 0x10u,      // Source data had more points per curve than the limit and some points were trimmed
        DataFlags_HasPointMapping = 0x20u,		// Contains platform to source points mapping data
        DataFlags_HasCurveMapping = 0x40u,		// Contains platform to source curves mapping data
        DataFlags_HasShuffledCurves = 0x80u,	// Curve data has been shuffled
        DataFlags_HasExtraEndingPoint = 0x100u	// Curve contains an extra ending control point
    };

    public class FHeader(FArchive Ar)
    {
        public uint CurveCount = Ar.Read<uint>();
        public uint PointCount = Ar.Read<uint>();
        public float MaxLength = Ar.Read<float>();
        public float MaxRadius = Ar.Read<float>();
        public FBox BoundingBox = new FBox(Ar);
        public uint MinPointPerCurve = Ar.Read<uint>();
        public uint MaxPointPerCurve = Ar.Read<uint>();
        public uint AvgPointPerCurve = Ar.Read<uint>();
        public EDataFlags Flags = Ar.Read<EDataFlags>();
        public uint[] CurveAttributeOffsets = Ar.ReadArray<uint>(UGroomAsset.HAIR_CURVE_ATTRIBUTE_COUNT);
        public uint[] PointAttributeOffsets = Ar.ReadArray<uint>(UGroomAsset.HAIR_POINT_ATTRIBUTE_COUNT);
        public uint ImportedAttributes = Ar.Read<uint>();
        public uint ImportedAttributeFlags = Ar.Read<uint>();
        /** Imported (Source) point/curve info */
        public uint SourceCurveCount = Ar.Game >= GAME_UE5_6 ? Ar.Read<uint>() : 0;
        public uint SourcePointCount = Ar.Game >= GAME_UE5_6 ? Ar.Read<uint>() : 0;
        public uint[] CurveToPointCount = Ar.ReadArray<uint>();
        public float[] CoverageScales = Ar.ReadArray<float>();
        public FStrides Strides = new FStrides(Ar);
        public FTranscoding Transcoding = Ar.Read<FTranscoding>();

        [StructLayout(LayoutKind.Sequential, Pack = 4)]
        public struct FTranscoding
        {
            public FVector PositionOffset;
            public FVector PositionScale;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 4)]
        public struct FStrides
        {
            public uint PositionStride;
            public uint CurveStride;
            public uint PointToCurveChunkStride;
            public uint CurveAttributeChunkStride;
            public uint PointAttributeChunkStride;
            public uint TranscodedPositionChunkStride;
            public uint CurveMappingStride;
            public uint PointMappingStride;

            public uint PointToCurveChunkElementCount;
            public uint CurveAttributeChunkElementCount;
            public uint PointAttributeChunkElementCount;
            public uint TranscodedPositionChunkElementCount;

            public FStrides(FArchive Ar)
            {
                PositionStride = Ar.Read<uint>();
                CurveStride = Ar.Read<uint>();
                PointToCurveChunkStride = Ar.Read<uint>();
                CurveAttributeChunkStride = Ar.Read<uint>();
                PointAttributeChunkStride = Ar.Read<uint>();
                TranscodedPositionChunkStride = Ar.Read<uint>();
                CurveMappingStride = Ar.Game >= GAME_UE5_6 ? Ar.Read<uint>() : 0;
                PointMappingStride = Ar.Game >= GAME_UE5_6 ? Ar.Read<uint>() : 0;

                PointToCurveChunkElementCount = Ar.Read<uint>();
                CurveAttributeChunkElementCount = Ar.Read<uint>();
                PointAttributeChunkElementCount = Ar.Read<uint>();
                TranscodedPositionChunkElementCount = Ar.Read<uint>();
            }
        }
    }

    public class FData
    {
        public FByteBulkData? Positions;
        public FByteBulkData? TranscodedPositions;
        public FByteBulkData CurveAttributes;
        public FByteBulkData PointAttributes;
        public FByteBulkData PointToCurve;
        public FByteBulkData Curves;
        public FByteBulkData PointMapping;
        public FByteBulkData CurveMapping;

        public FData(FAssetArchive Ar, ref FHeader header)
        {
            if (header.Flags.HasFlag(EDataFlags.DataFlags_HasTranscodedPosition))
            {
                TranscodedPositions = new FByteBulkData(Ar);
            }
            else
            {
                Positions = new FByteBulkData(Ar);
            }
            CurveAttributes = new FByteBulkData(Ar);
            if (header.Flags.HasFlag(EDataFlags.DataFlags_HasPointAttribute))
            {
                PointAttributes = new FByteBulkData(Ar);
            }
            PointToCurve = new FByteBulkData(Ar);
            Curves = new FByteBulkData(Ar);
            if (header.Flags.HasFlag(EDataFlags.DataFlags_HasPointMapping))
            {
                PointMapping = new FByteBulkData(Ar);
            }
            if (header.Flags.HasFlag(EDataFlags.DataFlags_HasCurveMapping))
            {
                CurveMapping = new FByteBulkData(Ar);
            }
        }
    }

    public override void SerializeHeader(FAssetArchive Ar)
    {
        Header = new FHeader(Ar);
    }

    public override void SerializeData(FAssetArchive Ar)
    {
        var CurveCount = Header?.CurveCount ?? 0;
        var PointCount = CurveCount > 0 ? Header?.CurveToPointCount[CurveCount - 1] : 0;
        if (Header?.Flags.HasFlag(EDataFlags.DataFlags_HasData) == false)
            return;

        Data = new FData(Ar, ref Header!);
    }
}

public class FHairStrandsBulkCommon
{
    public void Serialize(FAssetArchive Ar)
    {
        SerializeHeader(Ar);
        SerializeData(Ar);
    }
    public virtual void SerializeHeader(FAssetArchive Ar) { }

	public virtual void SerializeData(FAssetArchive Ar) { }
}
