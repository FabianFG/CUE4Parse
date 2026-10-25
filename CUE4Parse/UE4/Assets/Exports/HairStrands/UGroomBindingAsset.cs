using CUE4Parse.UE4.Assets.Objects;
using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Readers;
using CUE4Parse.UE4.Versions;
using Newtonsoft.Json;

namespace CUE4Parse.UE4.Assets.Exports.HairStrands;

public class UGroomBindingAsset : UObject
{
    public EGroomClassStripFlags StripFlags;
    public FHairGroupData[]? HairGroupDatas;
    public FHairGroupBulkData[]? HairGroupBulkDatas;
    public FHairGroupPlatformData[]? HairGroupsPlatformData;

    public static bool UsesEarlyAccessSerialization(EGame game) => game is < GAME_UE5_0 or GAME_UE5_EA_Legacy or GAME_UE5_EA;

    public override void Deserialize(FAssetArchive Ar, long validPos)
    {
        base.Deserialize(Ar, validPos);

#if DEBUG
        Log.Debug(nameof(UGroomBindingAsset));
#endif

        if (UsesEarlyAccessSerialization(Ar.Game))
        {
            HairGroupDatas = Ar.ReadArray(() => new FHairGroupData(Ar));
        }
        else
        {
            StripFlags = Ar.Read<EGroomClassStripFlags>();
            if (Ar.Game >= GAME_UE5_3)
            {
                HairGroupsPlatformData = Ar.ReadArray(() => new FHairGroupPlatformData(Ar, StripFlags));
            }
            else
            {
                HairGroupBulkDatas = Ar.ReadArray(() => new FHairGroupBulkData(Ar, StripFlags));
            }
        }
    }

    protected internal override void WriteJson(JsonWriter writer, JsonSerializer serializer)
    {
        base.WriteJson(writer, serializer);

        if (HairGroupDatas != null)
        {
            writer.WritePropertyName(nameof(HairGroupDatas));
            serializer.Serialize(writer, HairGroupDatas);
            return;
        }

        if (HairGroupBulkDatas != null)
        {
            writer.WritePropertyName(nameof(HairGroupBulkDatas));
            serializer.Serialize(writer, HairGroupBulkDatas);
        }

        if (HairGroupsPlatformData != null)
        {
            writer.WritePropertyName(nameof(HairGroupsPlatformData));
            serializer.Serialize(writer, HairGroupsPlatformData);
        }
    }

    // Used after UE5.2
    public class FHairGroupPlatformData
    {
        public FHairStrandsRootBulkData[] SimRootBulkDatas;
        public FHairStrandsRootBulkData[] RenRootBulkDatas;
        public FHairStrandsRootBulkData[][] CardsRootBulkDatas;

        // The minimum mesh LOD that this binding data can support
        public int TargetMeshMinLOD = -1;

        public FHairGroupPlatformData(FAssetArchive Ar, EGroomClassStripFlags flags)
        {
            SimRootBulkDatas = Ar.ReadArray(() => new FHairStrandsRootBulkData(Ar));
            bool bStripped = flags.HasFlag(EGroomClassStripFlags.CDSF_StrandsStripped);
            if (!bStripped)
            {
                RenRootBulkDatas = Ar.ReadArray(() => new FHairStrandsRootBulkData(Ar));
            }
            CardsRootBulkDatas = Ar.ReadArray(() => Ar.ReadArray(() => new FHairStrandsRootBulkData(Ar)));

            if (Ar.Game >= GAME_UE5_6) TargetMeshMinLOD = Ar.Read<int>();
        }

        [Flags]
        public enum EDataFlags : uint
        {
            DataFlags_HasData = 0x01, // Contains valid data. Otherwise: Position, Attributes, ... are all empty
            DataFlags_HasPointAttribute = 0x02, // Contains point attribute data.
            DataFlags_HasTranscodedPosition = 0x04, // Contains transcoded position
            DataFlags_HasTrimmedCurve = 0x08, // Source data had more curves per group than the limit and some curves were trimmed
            DataFlags_HasTrimmedPoint = 0x10, // Source data had more points per curve than the limit and some points were trimmed
            DataFlags_HasPointMapping = 0x20, // Contains platform to source points mapping data
            DataFlags_HasCurveMapping = 0x40, // Contains platform to source curves mapping data
            DataFlags_HasShuffledCurves = 0x80, // Curve data has been shuffled
            DataFlags_HasExtraEndingPoint = 0x100 // Curve contains an extra ending control point
        }

        public class FHairStrandsCommonDatas
        {
            public struct FHash
            {
                public ulong PositionHash;
                public ulong CurveCountHash;
                public ulong CurveOffsetHash;

                public override bool Equals(object? obj)
                {
                    return obj is FHash other &&
                           PositionHash == other.PositionHash &&
                           CurveCountHash == other.CurveCountHash &&
                           CurveOffsetHash == other.CurveOffsetHash;
                }

                public override int GetHashCode()
                {
                    return HashCode.Combine(PositionHash, CurveCountHash, CurveOffsetHash);
                }

                public static bool operator ==(FHash left, FHash right) => left.Equals(right);
                public static bool operator !=(FHash left, FHash right) => !left.Equals(right);
            }
        }

        public abstract class FHairStrandsBulkCommon;

        public class FHairStrandsRootBulkData : FHairStrandsBulkCommon
        {
            public FHeader Header;
            public FData Data;

            public FHairStrandsRootBulkData(FAssetArchive Ar)
            {
                Header = new FHeader(Ar);
                Data = new FData(Ar, Header);
            }

            public uint GetRootCount() => Header.RootCount;

            public bool IsValid => Header.RootCount > 0;

            public bool HasFlags(EDataFlags flags) => Header.Flags.HasFlag(flags);

            public class FHeader
            {
                public int LODIndex;
                public uint RootCount;
                public uint PointCount;
                public uint SampleCount;
                public uint MeshSectionCount;
                public uint UniqueTriangleCount;
                public EDataFlags Flags;
                public FHairStrandsCommonDatas.FHash Hash;
                public uint[]? UniqueSectionIndices;
                public FStrides Strides;

                public FLOD[]? LODs;

                public struct FStrides
                {
                    public uint RootToUniqueTriangleIndexBufferStride;
                    public uint RootBarycentricBufferStride;
                    public uint UniqueTriangleIndexBufferStride;
                    public uint RestUniqueTrianglePositionBufferStride;

                    public uint MeshInterpolationWeightsBufferStride;
                    public uint MeshSampleIndicesAndSectionsBufferStride;
                    public uint RestSamplePositionsBufferStride;
                }

                public class FLOD
                {
                    public int LODIndex = -1;
                    public uint UniqueTriangleCount = 0;
                    public uint SampleCount = 0; // Number of samples used for the mesh interpolation

                    public uint[]
                        UniqueSectionIndices; // Store the mesh section indices which are relevant for this root LOD data

                    public FLOD(FArchive Ar)
                    {
                        UniqueTriangleCount = Ar.Read<uint>();
                        SampleCount = Ar.Read<uint>();
                        UniqueSectionIndices = Ar.ReadArray<uint>();
                    }
                };

                public FHeader(FArchive Ar)
                {
                    if (Ar.Game < GAME_UE5_4)
                    {
                        RootCount = Ar.Read<uint>();
                        PointCount = Ar.Read<uint>();
                        Strides = Ar.Read<FStrides>();
                        LODs = Ar.ReadArray(() => new FLOD(Ar));
                        for (var i = 0; i < LODs.Length; i++) LODs[i].LODIndex = i;
                        return;
                    }

                    RootCount = Ar.Read<uint>();
                    PointCount = Ar.Read<uint>();
                    LODIndex = Ar.Read<int>();
                    UniqueTriangleCount = Ar.Read<uint>();
                    SampleCount = Ar.Read<uint>();
                    UniqueSectionIndices = Ar.ReadArray<uint>();
                    MeshSectionCount = Ar.Read<uint>();
                    if (Ar.Game >= GAME_UE5_8) Flags = Ar.Read<EDataFlags>();
                    if (Ar.Game >= GAME_UE5_8) Hash = Ar.Read<FHairStrandsCommonDatas.FHash>();
                    Strides = Ar.Read<FStrides>();
                }
            }

            public class FData
            {
                // Binding
                public FByteBulkData RootToUniqueTriangleIndexBuffer;
                public FByteBulkData RootBarycentricBuffer;
                public FByteBulkData UniqueTriangleIndexBuffer;
                public FByteBulkData RestUniqueTrianglePositionBuffer;

                // RBF
                public FByteBulkData? MeshInterpolationWeightsBuffer;
                public FByteBulkData? MeshSampleIndicesAndSectionsBuffer;
                public FByteBulkData? RestSamplePositionsBuffer;

                public FData(FAssetArchive Ar, FHeader header)
                {
                    RootToUniqueTriangleIndexBuffer = new FByteBulkData(Ar);
                    RootBarycentricBuffer = new FByteBulkData(Ar);
                    UniqueTriangleIndexBuffer = new FByteBulkData(Ar);
                    RestUniqueTrianglePositionBuffer = new FByteBulkData(Ar);

                    if (header.SampleCount > 0)
                    {
                        MeshInterpolationWeightsBuffer = new FByteBulkData(Ar);
                        MeshSampleIndicesAndSectionsBuffer = new FByteBulkData(Ar);
                        RestSamplePositionsBuffer = new FByteBulkData(Ar);
                    }
                }
            }
        }
    }
}

/* UGroomAsset::EGroomClassStripFlags */
[Flags]
public enum EGroomClassStripFlags : uint
{
    CDSF_None = 0,
    CDSF_ImportedStrands = 1,
    CDSF_MinLodData = 2,
    CDSF_StrandsStripped = 4,
    CDSF_CardsStripped = 8,
    CDSF_MeshesStripped = 16
}
