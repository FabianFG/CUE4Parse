using System;
using System.Collections.Immutable;
using System.IO;
using System.Numerics;
using System.Runtime.CompilerServices;
using CUE4Parse.UE4.Assets.Objects;
using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Objects.Core.Math;
using CUE4Parse.UE4.Objects.Engine;
using CUE4Parse.UE4.Readers;
using CUE4Parse.UE4.Versions;
using Newtonsoft.Json;

namespace CUE4Parse.UE4.Assets.Exports.Nanite;

public class NaniteUtils
{
    // nanite constants
    /// <summary>The maximum amount of UVs a nanite mesh can have</summary>
    public const int NANITE_MAX_UVS = 4;
    /// <summary>The maximum number of bits used to serialize normals.</summary>
    public static int NANITE_MAX_NORMAL_QUANTIZATION_BITS(EGame ver)
    {
        if (ver >= EGame.GAME_UE5_2) return 15;
        return 9;
    }
    /// <summary>The maximum number of bits used to serialize tangents.</summary>
    public const int NANITE_MAX_TANGENT_QUANTIZATION_BITS = 12;
    /// <summary>The maximum number of bits used to serialize an axis in a UV.</summary>
    public const int NANITE_MAX_TEXCOORD_QUANTIZATION_BITS = 15;
    /// <summary>The maximum number of bits used to serialize a color channel for a vertex color.</summary>
    public const int NANITE_MAX_COLOR_QUANTIZATION_BITS = 8;

    public const int NANITE_MAX_CLUSTERS_PER_PAGE_BITS = 8;
    /// <summary>The maximum amount of clusters that can be contained in a page.</summary>
    public const int NANITE_MAX_CLUSTERS_PER_PAGE = 1 << NANITE_MAX_CLUSTERS_PER_PAGE_BITS;

    public const int NANITE_MAX_CLUSTER_INDICES_BITS = 8;
    /// <summary>The maximum amount of tri indices that can be contained in a cluster.</summary>
    public const int NANITE_MAX_CLUSTER_INDICES = 1 << NANITE_MAX_CLUSTER_INDICES_BITS;
    public const int NANITE_MAX_CLUSTER_INDICES_MASK = NANITE_MAX_CLUSTER_INDICES - 1;

    /// <summary>The minimum multiplier used to compute the position delta of a vertex within a cluster.</summary>
    public static int NANITE_MIN_POSITION_PRECISION(EGame ver)
    {
        if (ver >= EGame.GAME_UE5_4)
            return -20;
        return -8;
    }
    /// <summary>The maximum multiplier used to compute the position delta of a vertex within a cluster.</summary>
    public static int NANITE_MAX_POSITION_PRECISION(EGame ver)
    {
        if (ver >= EGame.GAME_UE5_4)
            return 43;
        return 23;
    }

    public const int NANITE_MAX_BVH_NODE_FANOUT_BITS = 2;
    public const int NANITE_MAX_BVH_NODE_FANOUT = 1 << NANITE_MAX_BVH_NODE_FANOUT_BITS;

    public const int NANITE_MAX_CLUSTERS_PER_GROUP_BITS = 9;
    public const int NANITE_MAX_RESOURCE_PAGES_BITS = 20;

    public const int NANITE_MAX_HIERACHY_CHILDREN_BITS = 6;
    public const int NANITE_MAX_GROUP_PARTS_BITS = 3;
    public const int NANITE_MAX_HIERACHY_CHILDREN = (1 << NANITE_MAX_HIERACHY_CHILDREN_BITS);
    public const int NANITE_MAX_GROUP_PARTS_MASK = ((1 << NANITE_MAX_GROUP_PARTS_BITS) - 1);

    public readonly static ImmutableDictionary<int, float> PrecisionScales;

    static NaniteUtils()
    {
        ImmutableDictionary<int, float>.Builder builder = ImmutableDictionary.CreateBuilder<int, float>();
        for (int i = -32; i <= 32; i++)
        {
            int temp = 0;
            Unsafe.As<int, float>(ref temp) = 1.0f;
            float scale = 1.0f;
            Unsafe.As<float, int>(ref scale) = temp - (i << 23);
            builder.Add(i, scale);
        }
        PrecisionScales = builder.ToImmutable();
    }

    /// <summary>Equivalent to BitFieldExtractU32.</summary>
    public static uint GetBits(uint value, int numBits, int offset)
    {
        uint mask = (1u << numBits) - 1u;
        return (value >> offset) & mask;
    }

    public static int UIntToInt(uint value, int bitLength)
    {
        return unchecked((int) (value << (32-bitLength)) ) >> (32-bitLength);
    }

    /// <summary>Equivalent to BitFieldExtractS32.</summary>
    public static int GetBitsAsSigned(uint value, int numBits, int offset)
    {
        return UIntToInt(GetBits(value, numBits, offset), numBits);
    }

    public static uint BitAlignU32(uint high, uint low, long shift)
    {
        shift = shift & 31u;
        uint result = low >> (int)shift;
        result |= shift > 0 ? (high << (32 - (int) shift)) : 0u;
        return result;
    }

    public static uint BitFieldMaskU32(int maskWidth, int maskLocation)
    {
        maskWidth &= 31;
        maskLocation &= 31;
        return ((1u << maskWidth) - 1) << maskLocation;
    }

    /// <summary>
    /// Reads a non-byte aligned uint from an archive.
    /// </summary>
    /// <param name="Ar">The archive to read from.</param>
    /// <param name="baseAddressInBytes">A byte aligned position use as an anchor.</param>
    /// <param name="bitOffset">The offset in bits from the aligned location.</param>
    /// <returns></returns>
    public static uint ReadUnalignedDword(FArchive Ar, long baseAddressInBytes, long bitOffset)
    {
        long byteAddress = baseAddressInBytes + (bitOffset >> 3);
        long alignedByteAddress = byteAddress & ~3;
        bitOffset = ((byteAddress - alignedByteAddress) << 3) | (bitOffset & 7);
        Ar.Position = alignedByteAddress;
        uint low = Ar.Read<uint>();
        uint high = Ar.Read<uint>();
        return BitAlignU32(high, low, bitOffset);
    }

    public static uint UnpackByte0(uint v) => v & 0xff;
    public static uint UnpackByte1(uint v) => (v >> 8) & 0xff;
    public static uint UnpackByte2(uint v) => (v >> 16) & 0xff;
    public static uint UnpackByte3(uint v) => v >> 24;

    /// <summary>
    /// Finds the location of the highest populated bit in a uint.
    /// Essentially just the ReverseBitScan instruction with a defined behaviour if the value is 0.
    /// </summary>
    /// <returns>the index of the first bit or uint.MAX_VALUE if not found.</returns>
    public static uint FirstBitHigh(uint x)
    {
        return x == 0 ? 0xFFFFFFFFu : (uint) BitOperations.Log2(x);
    }

    public static BitStreamReader CreateBitStreamReader_Aligned(long byteAddress, long bitOffset, long compileTimeMaxRemainingBits)
    {
        return new BitStreamReader(byteAddress, bitOffset, compileTimeMaxRemainingBits);
    }

    public static BitStreamReader CreateBitStreamReader(long byteAddress, long bitOffset, long compileTimeMaxRemainingBits)
    {
        long AlignedByteAddress = byteAddress & ~3;
        bitOffset += (byteAddress & 3) << 3;
        return new BitStreamReader(AlignedByteAddress, bitOffset, compileTimeMaxRemainingBits);
    }

    public class BitStreamReader
    {
        long AlignedByteAddress;
        long BitOffsetFromAddress;
        long CompileTimeMaxRemainingBits;

        uint[] BufferBits = [0, 0, 0, 0];
        long BufferOffset = 0;
        long CompileTimeMinBufferBits = 0;
        long CompileTimeMinDwordBits = 0;

        public BitStreamReader(long alignedByteAddress, long bitOffset, long compileTimeMaxRemainingBits)
        {
            AlignedByteAddress = alignedByteAddress;
            BitOffsetFromAddress = bitOffset;
            CompileTimeMaxRemainingBits = compileTimeMaxRemainingBits;
        }

        public uint Read(FArchive Ar, int numBits, int compileTimeMaxBits)
        {
            if (compileTimeMaxBits > CompileTimeMinBufferBits)
            {
                // BitBuffer could be out of bits: Reload.

                // Add cumulated offset since last refill. No need to update at every read.
                BitOffsetFromAddress += BufferOffset;
                long address = AlignedByteAddress + ((BitOffsetFromAddress >> 5) << 2);

                // You have to be a bit weird about it because it tries
                // to read from out of bounds, which is not great NGL
                Ar.Position = address;
                uint[] data = [0, 0, 0, 0];
                for (int i = 0; i < data.Length; i++)
                {
                    if (Ar.Position + sizeof(uint) <= Ar.Length)
                    {
                        data[i] = Ar.Read<uint>();
                    }
                    else if (Ar.Position == Ar.Length)
                    {
                        // safety
                        data[i] = 0;
                    }
                    else
                    {
                        uint value = 0u;
                        byte[] bytes = Ar.ReadBytes((int) Math.Min(sizeof(uint), Ar.Position - Ar.Length));
                        for (int j = 0; j < bytes.Length; j++)
                        {
                            value |= (uint) bytes[j] << (j * 8);
                        }
                        data[i] = value;
                    }
                }
                // Shift bits down to align
                BufferBits[0] = BitAlignU32(data[1], data[0], BitOffsetFromAddress); // BitOffsetFromAddress implicitly &31
                if (CompileTimeMaxRemainingBits > 32) BufferBits[1] = BitAlignU32(data[2], data[1], BitOffsetFromAddress); // BitOffsetFromAddress implicitly &31
                if (CompileTimeMaxRemainingBits > 64) BufferBits[2] = BitAlignU32(data[3], data[2], BitOffsetFromAddress); // BitOffsetFromAddress implicitly &31
                if (CompileTimeMaxRemainingBits > 96) BufferBits[3] = BitAlignU32(0, data[3], BitOffsetFromAddress); // BitOffsetFromAddress implicitly &31

                BufferOffset = 0;
                CompileTimeMinDwordBits = Math.Min(32, CompileTimeMaxRemainingBits);
                CompileTimeMinBufferBits = Math.Min(97, CompileTimeMaxRemainingBits); // Up to 31 bits wasted to alignment
        
            }
            else if (compileTimeMaxBits > CompileTimeMinDwordBits)
            {
                // Bottom dword could be out of bits: Shift down.
                BitOffsetFromAddress += BufferOffset;

                // Workaround for BitAlignU32(x, y, 32) returning x instead of y.
                // In the common case where State.CompileTimeMinDwordBits != 0, this will be optimized to just BitAlignU32.
                // sTODO: Can we get rid of this special case?
                bool offset32 = CompileTimeMinDwordBits == 0 && BufferOffset == 32;

                BufferBits[0]                                    = offset32 ? BufferBits[1] : BitAlignU32(BufferBits[1], BufferBits[0], BufferOffset);
                if (CompileTimeMinBufferBits > 32) BufferBits[1] = offset32 ? BufferBits[2] : BitAlignU32(BufferBits[2], BufferBits[1], BufferOffset);
                if (CompileTimeMinBufferBits > 64) BufferBits[2] = offset32 ? BufferBits[3] : BitAlignU32(BufferBits[3], BufferBits[2], BufferOffset);
                if (CompileTimeMinBufferBits > 96) BufferBits[3] = offset32 ? 0             : BitAlignU32(0,             BufferBits[3], BufferOffset);
        
                BufferOffset = 0;

                CompileTimeMinDwordBits = Math.Min(32, CompileTimeMaxRemainingBits);
            }

            uint result = GetBits(BufferBits[0], numBits, (int)BufferOffset); // BufferOffset implicitly &31
            BufferOffset += numBits;
            CompileTimeMinBufferBits -= compileTimeMaxBits;
            CompileTimeMinDwordBits -= compileTimeMaxBits;
            CompileTimeMaxRemainingBits -= compileTimeMaxBits;

            return result;
        }
    }

}

public class FHierarchyNodeSlice
{
    public FVector4 LODBounds;
    public FVector BoxBoundsCenter;
    public FVector BoxBoundsExtent;
    public float MinLODError;
    public float MaxParentLODError;
    public uint ChildStartReference;    // Can be node (index) or cluster (page:cluster)
    public uint NumChildren;
    public uint StartPageIndex;
    public uint NumPages;
    public uint AssemblyTransformIndex;
    public bool bEnabled;
    public bool bLoaded;
    public bool bLeaf;
    public uint NodeIndex;
    public uint SliceIndex;

    public FHierarchyNodeSlice(FArchive Ar, uint index, uint sliceIndex)
    {
        NodeIndex = index;
        SliceIndex = sliceIndex;
        LODBounds = Ar.Read<FVector4>();
        BoxBoundsCenter = Ar.Read<FVector>();
        MinLODError = (float) Ar.Read<Half>();
        MaxParentLODError = (float) Ar.Read<Half>();
        BoxBoundsExtent = Ar.Read<FVector>();
        ChildStartReference = Ar.Read<uint>();
        bLoaded = ChildStartReference != 0xFFFFFFFFu;
        var misc2 = Ar.Read<uint>();
        NumChildren = NaniteUtils.GetBits(misc2, NaniteUtils.NANITE_MAX_CLUSTERS_PER_GROUP_BITS, 0);
        NumPages = NaniteUtils.GetBits(misc2, NaniteUtils.NANITE_MAX_GROUP_PARTS_BITS, NaniteUtils.NANITE_MAX_CLUSTERS_PER_GROUP_BITS);
        StartPageIndex = NaniteUtils.GetBits(misc2, NaniteUtils.NANITE_MAX_RESOURCE_PAGES_BITS, NaniteUtils.NANITE_MAX_CLUSTERS_PER_GROUP_BITS + NaniteUtils.NANITE_MAX_GROUP_PARTS_BITS);
        bEnabled = misc2 != 0u;
        bLeaf = misc2 != 0xFFFFFFFFu;
        // #if NANITE_ASSEMBLY_DATA 5.6+ but set to 0
        //         Ar << Node.Misc2[ i ].AssemblyPartIndex;
        // #endif
    }
}

public class FPackedHierarchyNode
{
    public FHierarchyNodeSlice[] Slices;

    public FPackedHierarchyNode(FArchive Ar, uint index)
    {
        Slices = new FHierarchyNodeSlice[NaniteUtils.NANITE_MAX_BVH_NODE_FANOUT];
        for (uint i = 0; i < NaniteUtils.NANITE_MAX_BVH_NODE_FANOUT; i++)
        {
            Slices[i] = new FHierarchyNodeSlice(Ar, index, i);
        }
    }
}

public class FPageStreamingState
{
    public uint BulkOffset;
    public uint BulkSize;
    public uint PageSize;
    public uint DependenciesStart;
    public uint DependenciesNum;
    public byte MaxHierarchyDepth;
    public uint Flags;

    public FPageStreamingState(FAssetArchive Ar)
    {
        BulkOffset = Ar.Read<uint>();
        BulkSize = Ar.Read<uint>();
        PageSize = Ar.Read<uint>();
        DependenciesStart = Ar.Read<uint>();
        if (Ar.Game >= EGame.GAME_UE5_3)
        {
            DependenciesNum = Ar.Read<ushort>();
            MaxHierarchyDepth = Ar.Read<byte>();
            Flags = Ar.Read<byte>();
        }
        else
        {
            DependenciesNum = Ar.Read<uint>();
            // doesn't exist in 5.0EA
            Flags = Ar.Ver >= EUnrealEngineObjectUE5Version.LARGE_WORLD_COORDINATES ? Ar.Read<uint>() : 0;
        }
    }
}

public class FFixupChunk
{
    public class FHeader
    {
        public readonly ushort NumClusters;
        public readonly ushort NumHierachyFixups;
        public readonly ushort NumClusterFixups;

        public FHeader(FArchive Ar)
        {
            // the NF header was add in 5.3 in previous versions it isn't there
            if (Ar.Game >= EGame.GAME_UE5_3)
            {
                ushort magic = Ar.Read<ushort>();
                if (magic != 0x464Eu) //NF
                {
                    throw new InvalidDataException($"Invalid magic value, expected {0x464Eu:04x} got {magic:04x}");
                }
            }
            NumClusters = Ar.Read<ushort>();
            NumHierachyFixups = Ar.Read<ushort>();
            NumClusterFixups = Ar.Read<ushort>();
            // prior to that the end was just padding
            if (Ar.Game < EGame.GAME_UE5_3)
            {
                // 2 byte padding
                Ar.Position += 2;
            }
        }
    }

    public readonly FHeader Header;
    public FHierarchyFixup[] HierarchyFixups;
    public FClusterFixup[] ClusterFixups;

    public FFixupChunk(FArchive Ar)
    {
        Header = new FHeader(Ar);
        HierarchyFixups = Ar.ReadArray(Header.NumHierachyFixups, () => new FHierarchyFixup(Ar));
        ClusterFixups = Ar.ReadArray(Header.NumClusterFixups, () => new FClusterFixup(Ar));
    }
}

public class FHierarchyFixup
{

    public uint PageIndex;
    public uint NodeIndex;
    public uint ChildIndex;
    public uint ClusterGroupPartStartIndex;
    public uint PageDependencyStart;
    public uint PageDependencyNum;

    public FHierarchyFixup(FArchive Ar)
    {
        PageIndex = Ar.Read<uint>();

        var hierarchyNodeAndChildIndex = Ar.Read<uint>();
        NodeIndex = hierarchyNodeAndChildIndex >> NaniteUtils.NANITE_MAX_HIERACHY_CHILDREN_BITS;
        ChildIndex = hierarchyNodeAndChildIndex & (NaniteUtils.NANITE_MAX_HIERACHY_CHILDREN - 1);

        ClusterGroupPartStartIndex = Ar.Read<uint>();

        var pageDependencyStartAndNum = Ar.Read<uint>();
        PageDependencyStart = pageDependencyStartAndNum >> NaniteUtils.NANITE_MAX_GROUP_PARTS_BITS;
        PageDependencyNum = pageDependencyStartAndNum & NaniteUtils.NANITE_MAX_GROUP_PARTS_MASK;
    }
}

public class FNaniteVertexAttributes
{
    /// <summary>The normals of the vertex.</summary>
    public FVector Normal;
    /// <summary>The tangent component of the vertex + the sign as w (tx,ty,tz,sign).</summary>
    public FVector4 TangentXAndSign;
    /// <summary>The color of the vertex.</summary>
    public FVector4 Color;
    /// <summary>The uv coordinates of the vertex.</summary>
    public FVector2D[] UVs = new FVector2D[NaniteUtils.NANITE_MAX_UVS];
}

public readonly struct FUVRange
{
    public readonly int MinX;
    public readonly int MinY;
    public readonly uint GapStartX;
    public readonly uint GapStartY;
    public readonly int GapLengthX;
    public readonly int GapLengthY;
    public readonly int Precision;
    [JsonIgnore]
    public readonly uint Padding;
}

public class FNaniteVertex
{
    /// <summary>The position value referenced for reference vertices</summary>
    public FIntVector RawPos;
    /// <summary>The position of the vertex in the 3d world.</summary>
    public FVector Pos;
    /// <summary>The attributes of the vertex.</summary>
    public FNaniteVertexAttributes? Attributes;
    /// <summary>True if the vertex as read as a reference. This exists only for debugging purposes.</summary>
    public bool IsRef;

    private static FVector UnpackNormals(uint packed, int bits)
    {
        uint mask = NaniteUtils.BitFieldMaskU32(bits, 0);
        float[] f = [
            NaniteUtils.GetBits(packed, bits, 0) * (2.0f / mask) - 1.0f,
            NaniteUtils.GetBits(packed, bits, bits) * (2.0f / mask) - 1.0f
         ];
        FVector n = new(f[0], f[1], 1.0f - Math.Abs(f[0]) - Math.Abs(f[1]));
        float t = Math.Clamp(-n[2], 0.0f, 1.0f);
        n.X += n.X >= 0 ? -t : t;
        n.Y += n.Y >= 0 ? -t : t;
        n.Normalize();
        return n;
    }

    private static FVector UnpackTangentX(FVector tangentZ, uint tangentAngleBits, int numTangentBits)
    {
        bool swapXZ = Math.Abs(tangentZ.Z) > Math.Abs(tangentZ.X);
        if (swapXZ)
        {
            tangentZ = new FVector(tangentZ.Z, tangentZ.Y, tangentZ.X);
        }

        FVector tangentRefX = new FVector(-tangentZ.Y, tangentZ.X, 0.0f);
        FVector tangentRefY = tangentZ ^ tangentRefX;

        float dot = 0.0f;
        dot += tangentRefX.X * tangentRefX.X;
        dot += tangentRefX.Y * tangentRefX.Y;
        float scale = 1.0f / (float)Math.Sqrt(dot);

        float tangentAngle = tangentAngleBits * (float)(2.0 * Math.PI) / (1 << numTangentBits);
        FVector tangentX = tangentRefX * (float)(Math.Cos(tangentAngle) * scale) + tangentRefY * (float)(Math.Sin(tangentAngle) * scale);
        if (swapXZ)
        {
            (tangentX.X, tangentX.Z) = (tangentX.Z, tangentX.X);
        }
        return tangentX;
    }

    private static FVector2D UnpackTexCoord(uint[] packed, FUVRange uvRange)
    {
        if (packed.Length != 2)
        {
            throw new ArgumentException($"{nameof(packed)} wasn't of length 2!");
        }
        FVector2D t = new(packed[0], packed[1]);
        t += new FVector2D(
            packed[0] > uvRange.GapStartX ? uvRange.GapLengthX : 0.0f,
            packed[1] > uvRange.GapStartY ? uvRange.GapLengthY : 0.0f
        );
        float scale = NaniteUtils.PrecisionScales[uvRange.Precision];
        return (t + new FVector2D(uvRange.MinX, uvRange.MinY)) * scale;
    }

    internal void ReadPosData(FArchive Ar, long srcBaseAddress, long srcBitOffset, FCluster cluster)
    {
        uint NumBits = cluster.PosBitsX + cluster.PosBitsY + cluster.PosBitsZ;
        uint[] packed = [
            NaniteUtils.ReadUnalignedDword(Ar, srcBaseAddress, srcBitOffset),
            NaniteUtils.ReadUnalignedDword(Ar, srcBaseAddress, srcBitOffset + 32)
        ];

        uint[] rawPos = [0, 0, 0];

        rawPos[0] = NaniteUtils.GetBits(packed[0], (int)cluster.PosBitsX, 0);
        packed[0] = NaniteUtils.BitAlignU32(packed[1], packed[0], cluster.PosBitsX);
        packed[1] >>= (int)cluster.PosBitsX;

        rawPos[1] = NaniteUtils.GetBits(packed[0], (int) cluster.PosBitsY, 0);
        packed[0] = NaniteUtils.BitAlignU32(packed[1], packed[0], cluster.PosBitsY);

        rawPos[2] = NaniteUtils.GetBits(packed[0], (int) cluster.PosBitsZ, 0);

       
        RawPos = new FIntVector((int) rawPos[0], (int) rawPos[1], (int) rawPos[2]);
        Pos = new FVector(rawPos[0], rawPos[1], rawPos[2]);
        Pos = (RawPos + cluster.PosStart) * NaniteUtils.PrecisionScales[cluster.PosPrecision];
    }

    internal void ReadAttributeData(FArchive Ar, NaniteUtils.BitStreamReader bitStreamReader, FNaniteStreamableData page, FCluster cluster, int clusterIndex)
    {
        Attributes = new FNaniteVertexAttributes();

        long decodeInfoOffset = page.GPUPageHeaderOffset + cluster.DecodeInfoOffset;

        uint[] colorMin = [NaniteUtils.UnpackByte0(cluster.ColorMin), NaniteUtils.UnpackByte1(cluster.ColorMin), NaniteUtils.UnpackByte2(cluster.ColorMin), NaniteUtils.UnpackByte3(cluster.ColorMin)];

        int[] numComponentBits = [
            (int) NaniteUtils.GetBits(cluster.ColorBits, 4, 0),
            (int) NaniteUtils.GetBits(cluster.ColorBits, 4, 4),
            (int) NaniteUtils.GetBits(cluster.ColorBits, 4, 8),
            (int) NaniteUtils.GetBits(cluster.ColorBits, 4, 12),
        ];

        // parses normals
        if (Ar.Game >= EGame.GAME_UE5_2)
        {
            uint normalBits = bitStreamReader.Read(Ar, 2 * (int) cluster.NormalPrecision, 2 * NaniteUtils.NANITE_MAX_NORMAL_QUANTIZATION_BITS(Ar.Game));
            Attributes.Normal = UnpackNormals(normalBits, (int) cluster.NormalPrecision);
        }
        else
        {
            int normalPrecision = NaniteUtils.NANITE_MAX_NORMAL_QUANTIZATION_BITS(Ar.Game);
            uint normalBits = bitStreamReader.Read(Ar, 2 * normalPrecision, 2 * normalPrecision);
            Attributes.Normal = UnpackNormals(normalBits, normalPrecision);
        }
        

        // parse tangent
        if (Ar.Game >= EGame.GAME_UE5_3)
        {
            int numTangentBits = cluster.HasTangents ? ((int) cluster.TangentPrecision + 1) : 0;
            uint tangentAngleAndSignBits = bitStreamReader.Read(Ar, numTangentBits, NaniteUtils.NANITE_MAX_TANGENT_QUANTIZATION_BITS + 1);
            if (cluster.HasTangents)
            {
                bool bTangentYSign = (tangentAngleAndSignBits & (1 << (int) cluster.TangentPrecision)) != 0;
                uint tangentAngleBits = NaniteUtils.GetBits(tangentAngleAndSignBits, (int) cluster.TangentPrecision, 0);
                FVector tangentX = UnpackTangentX(new FVector(Attributes.Normal.X, Attributes.Normal.Y, Attributes.Normal.Z), tangentAngleBits, (int) cluster.TangentPrecision);
                Attributes.TangentXAndSign = new FVector4(tangentX, bTangentYSign ? -1.0f : 1.0f);
            }
            else
            {
                Attributes.TangentXAndSign = new FVector4(0, 0, 0, 0);
            }
        }

        // parse color
        uint[] colorDelta = [
            bitStreamReader.Read(Ar, numComponentBits[0], NaniteUtils.NANITE_MAX_COLOR_QUANTIZATION_BITS),
            bitStreamReader.Read(Ar, numComponentBits[1], NaniteUtils.NANITE_MAX_COLOR_QUANTIZATION_BITS),
            bitStreamReader.Read(Ar, numComponentBits[2], NaniteUtils.NANITE_MAX_COLOR_QUANTIZATION_BITS),
            bitStreamReader.Read(Ar, numComponentBits[3], NaniteUtils.NANITE_MAX_COLOR_QUANTIZATION_BITS)
        ];
        // should be in the ranges of 0.0f .. 1.0f
        Attributes.Color = new FVector4(
            (colorMin[0] + colorDelta[0]) * (1.0f / 255.0f),
            (colorMin[1] + colorDelta[1]) * (1.0f / 255.0f),
            (colorMin[2] + colorDelta[2]) * (1.0f / 255.0f),
            (colorMin[3] + colorDelta[3]) * (1.0f / 255.0f)
        );

        // parse tex coords
        for (int texCoordIndex = 0; texCoordIndex < cluster.NumUVs; texCoordIndex++)
        {
            int[] uvPrec = [
                (int) NaniteUtils.GetBits(cluster.UV_Prec, 4, texCoordIndex * 8),
                (int) NaniteUtils.GetBits(cluster.UV_Prec, 4, texCoordIndex * 8 + 4)
            ];
            uint[] UVBits = [
                bitStreamReader.Read(Ar, uvPrec[0], NaniteUtils.NANITE_MAX_TEXCOORD_QUANTIZATION_BITS),
                bitStreamReader.Read(Ar, uvPrec[1], NaniteUtils.NANITE_MAX_TEXCOORD_QUANTIZATION_BITS)
            ];

            if (texCoordIndex < cluster.NumUVs)
            {
                Attributes.UVs[texCoordIndex] = UnpackTexCoord(UVBits, page.UVRanges![clusterIndex][texCoordIndex]);
            }
            else {
                Attributes.UVs[texCoordIndex] = new FVector2D(0.0f, 0.0f);
            }
        }
        // ensure that the unused tex coords are just 0,0
        for (uint texCoordIndex = cluster.NumUVs; texCoordIndex < NaniteUtils.NANITE_MAX_UVS; texCoordIndex++)
        {
            Attributes.UVs[texCoordIndex] = new FVector2D(0.0f, 0.0f);
        }
    }
}

public class FClusterFixup
{

    public uint PageIndex;
    public uint ClusterIndex;
    public uint PageDependencyStart;
    public uint PageDependencyNum;

    public FClusterFixup(FArchive Ar)
    {
        var pageAndClusterIndex = Ar.Read<uint>();
        PageIndex = pageAndClusterIndex >> NaniteUtils.NANITE_MAX_CLUSTERS_PER_PAGE_BITS;
        ClusterIndex = pageAndClusterIndex & (NaniteUtils.NANITE_MAX_CLUSTERS_PER_PAGE - 1u);

        var pageDependencyStartAndNum = Ar.Read<uint>();
        PageDependencyStart = pageDependencyStartAndNum >> NaniteUtils.NANITE_MAX_GROUP_PARTS_BITS;
        PageDependencyNum = pageDependencyStartAndNum & NaniteUtils.NANITE_MAX_GROUP_PARTS_MASK;
    }
}

public readonly struct FMaterialRange
{
    /// <summary>The index of the first triangle that uses this material.</summary>
    public readonly uint TriStart;
    /// <summary>The number of tirangles that use this material.</summary>
    public readonly uint TriLength;
    /// <summary>The index of the material used by the triangles this range points to.</summary>
    public readonly uint MaterialIndex;

    public FMaterialRange(uint data) : this (
        NaniteUtils.GetBits(data, 8, 0),
        NaniteUtils.GetBits(data, 8, 8),
        NaniteUtils.GetBits(data, 6, 16)
    )
    { }

    public FMaterialRange(uint triStart, uint triLength, uint materialIndex) {
        TriStart = triStart;
        TriLength = triLength;
        MaterialIndex = materialIndex;
    }
}

public class FCluster
{
    /// <summary>The number of vertexes in this cluster.</summary>
    public uint NumVerts;
    /// <summary>The offset from the gpu page header where the non-ref vertex positions can be found.</summary>
    public uint PositionOffset;
    /// <summary>The number of triangle indices in this cluster.</summary>
    public uint NumTris;
    /// <summary>The offset from the gpu page header where the index data can be found.</summary>
    public uint IndexOffset;
    /// <summary>A mimimum value for all vertex colors in this cluster. When reading a vertex color, add this value or else it will look off.</summary>
    public uint ColorMin;
    /// <summary>number of bits for each channel of the vertex colors, each taking 4 bits.</summary>
    public uint ColorBits;
    /// <summary>Debug value, never actually used.</summary>
    public uint GroupIndex;
    /// <summary>The "starting point" to which all vertices in this cluster are relative to.</summary>
    public FIntVector PosStart;
    /// <summary>The number of bits used per vertex index in the strip indices when the strip are optimized at runtime. Unused for our purposes.</summary>
    public uint BitsPerIndex;
    /// <summary>A multiplier used to scale the delta between the pos start and the vertex positions.</summary>
    public int PosPrecision;
    /// <summary>The number of bits used to encode the X coordinate of a vertex.</summary>
    public uint PosBitsX;
    /// <summary>The number of bits used to encode the Y coordinate of a vertex.</summary>
    public uint PosBitsY;
    /// <summary>The number of bits used to encode the Z coordinate of a vertex.</summary>
    public uint PosBitsZ;
    /// <summary>The number of bits used to encode the x and y coordinate of the normals. Only available after 5.2</summary>
    public uint NormalPrecision;
    /// <summary>The number of bits used to encode the tangent. Only available after 5.1.</summary>
    public uint TangentPrecision;
    public FVector4 LODBounds;
    public FVector BoxBoundsCenter;
    /// <summary>The lower the value the closer the cluster is to the original mesh.</summary>
    public float LODError;
    /// <summary>The lower the value the closer the cluster is to the original mesh.</summary>
    public float EdgeLength;
    public FVector BoxBoundsExtent;
    /// <summary>Flags used to identify the cluster's role.</summary>
    public uint Flags;
    /// <summary>The offset from the gpu page header where the vertex attributes of non-ref vertices can be found.</summary>
    public uint AttributeOffset;
    /// <summary>The number of bits used to store the atribute data for a non-ref vertex in this cluster.</summary>
    public uint BitsPerAttribute;
    /// <summary>The offset from the gpu page header where the uv ranges for this cluster can be found.</summary>
    public uint DecodeInfoOffset;
    /// <summary>True if the mesh has explicit tangents, only available after 5.3</summary>
    public bool HasTangents;
    /// <summary>The number of uv ranges associated with this cluster.</summary>
    public uint NumUVs;
    public uint ColorMode;
    /// <summary>A map of the number of bits which which uv positions are serialized as. each byte can be separated into 2 nibbles, each representing the x and y coordinates.</summary>
    public uint UV_Prec;
    /// <summary>The offset from the gpu page header where the material table for this cluser can be found. Will be 0 of the cluster does not use a material table.</summary>
    public uint MaterialTableOffset;
    /// <summary>The number of entries in the material table.</summary>
    public uint MaterialTableLength;
    /// <summary>Tri triangle index where the first material starts when not using a material table.</summary>
    public uint Material0Index;
    /// <summary>Tri triangle index where the second material starts when not using a material table.</summary>
    public uint Material1Index;
    /// <summary>Tri triangle index where the third material starts when not using a material table.</summary>
    public uint Material2Index;
    /// <summary>The number of triangles associated with the first material. 0 if the cluster uses a material table.</summary>
    public uint Material0Length;
    /// <summary>The number of triangles associated with the second material when not using a material table.</summary>
    public uint Material1Length;
    public uint VertReuseBatchCountTableOffset;
    public uint VertReuseBatchCountTableSize;
    public TIntVector4<uint> VertReuseBatchInfo;

    // decoded mesh data
    public FNaniteVertex?[] Vertices = [];
    public uint[][] TriIndices = [];
    public FMaterialRange[] MaterialRanges = [];
    public uint[] GroupRefToVertex = [];
    public uint[] GroupNonRefToVertex = [];

    public FCluster(FArchive Ar) : this(Ar, null, 1, 0)
    {
    }

    public FCluster(FArchive Ar, FNaniteStreamableData? page, int numClusters, int clusterIndex) {
        if (numClusters <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(numClusters), $"{nameof(numClusters)} should never be 0 or a negative value.");
        }
        if (clusterIndex < 0 || clusterIndex >= numClusters)
        {
            throw new ArgumentOutOfRangeException(nameof(clusterIndex), $"{nameof(numClusters)} should never be below 0 or greate than {nameof(numClusters)} ({numClusters})");
        }
        // clusters are stored in SOA layout so we gotta walk the stride.
        int stride = 16 * (numClusters - 1);

        var numVerts_positionOffset = Ar.Read<uint>();
        NumVerts = NaniteUtils.GetBits(numVerts_positionOffset, 9, 0);
        PositionOffset = NaniteUtils.GetBits(numVerts_positionOffset, 23, 9);

        var numTris_indexOffset = Ar.Read<uint>();
        NumTris = NaniteUtils.GetBits(numTris_indexOffset, 8, 0);
        IndexOffset = NaniteUtils.GetBits(numTris_indexOffset, 24, 8);

        ColorMin = Ar.Read<uint>();

        var colorBits_groupIndex = Ar.Read<uint>();
        ColorBits = NaniteUtils.GetBits(colorBits_groupIndex, 16, 0);
        GroupIndex = NaniteUtils.GetBits(colorBits_groupIndex, 16, 16); // debug only

        Ar.Position += stride;
        PosStart = Ar.Read<FIntVector>();

        var bitsPerIndex_posPrecision_posBits = Ar.Read<uint>();
        BitsPerIndex = NaniteUtils.GetBits(bitsPerIndex_posPrecision_posBits, 4, 0);
        PosPrecision = ((int) NaniteUtils.GetBits(bitsPerIndex_posPrecision_posBits, 5, 4)) + NaniteUtils.NANITE_MIN_POSITION_PRECISION(Ar.Game);
        PosBitsX = NaniteUtils.GetBits(bitsPerIndex_posPrecision_posBits, 5, 9);
        PosBitsY = NaniteUtils.GetBits(bitsPerIndex_posPrecision_posBits, 5, 14);
        PosBitsZ = NaniteUtils.GetBits(bitsPerIndex_posPrecision_posBits, 5, 19);
        if (Ar.Game >= EGame.GAME_UE5_2)
        {
            NormalPrecision = NaniteUtils.GetBits(bitsPerIndex_posPrecision_posBits, 4, 24);
        }
        if (Ar.Game >= EGame.GAME_UE5_3)
        {
            TangentPrecision = NaniteUtils.GetBits(bitsPerIndex_posPrecision_posBits, 4, 28);
        }
        

        Ar.Position += stride;
        LODBounds = Ar.Read<FVector4>();

        Ar.Position += stride;
        BoxBoundsCenter = Ar.Read<FVector>();
        LODError = (float) Ar.Read<Half>();
        EdgeLength = (float) Ar.Read<Half>();

        Ar.Position += stride;
        BoxBoundsExtent = Ar.Read<FVector>();
        Flags = Ar.Read<uint>();

        Ar.Position += stride;
        var attributeOffset_bitsPerAttribute = Ar.Read<uint>();
        AttributeOffset = NaniteUtils.GetBits(attributeOffset_bitsPerAttribute, 22, 0);
        BitsPerAttribute = NaniteUtils.GetBits(attributeOffset_bitsPerAttribute, 10, 22);

        var decodeInfoOffset_bHasTengants_numUVs_colorMode = Ar.Read<uint>();
        DecodeInfoOffset = NaniteUtils.GetBits(decodeInfoOffset_bHasTengants_numUVs_colorMode, 22, 0);
        if (Ar.Game >= EGame.GAME_UE5_3)
        {
            HasTangents = NaniteUtils.GetBits(decodeInfoOffset_bHasTengants_numUVs_colorMode, 1, 22) == 1;
            NumUVs = NaniteUtils.GetBits(decodeInfoOffset_bHasTengants_numUVs_colorMode, 3, 23);
            ColorMode = NaniteUtils.GetBits(decodeInfoOffset_bHasTengants_numUVs_colorMode, 2, 26);
        }
        else
        {
            NumUVs = NaniteUtils.GetBits(decodeInfoOffset_bHasTengants_numUVs_colorMode, 3, 22);
            ColorMode = NaniteUtils.GetBits(decodeInfoOffset_bHasTengants_numUVs_colorMode, 2, 25);
        }

        UV_Prec = Ar.Read<uint>();

        var materialEncoding = Ar.Read<uint>();
        if (materialEncoding < 0xFE000000u)
        {
            // fast path
            MaterialTableOffset = 0;
            MaterialTableLength	= 0;
            Material0Index = NaniteUtils.GetBits(materialEncoding, 6, 0);
            Material1Index = NaniteUtils.GetBits(materialEncoding, 6, 6);
            Material2Index = NaniteUtils.GetBits(materialEncoding, 6, 12);
            Material0Length = NaniteUtils.GetBits(materialEncoding, 7, 18) + 1;
            Material1Length = NaniteUtils.GetBits(materialEncoding, 7, 25);
            VertReuseBatchCountTableOffset = 0;
            VertReuseBatchCountTableSize = 0;

            Ar.Position += stride;
            VertReuseBatchInfo = Ar.Read<TIntVector4<uint>>();
            MaterialRanges = [];
        }
        else
        {
            // slow path
            MaterialTableOffset = NaniteUtils.GetBits(materialEncoding, 19, 0);
            MaterialTableLength = NaniteUtils.GetBits(materialEncoding, 6, 19) + 1;
            Material0Index = 0;
            Material1Index = 0;
            Material2Index = 0;
            Material0Length = 0;
            Material1Length = 0;
            VertReuseBatchCountTableOffset = Ar.Read<uint>();
            VertReuseBatchCountTableSize = Ar.Read<uint>();
            // we can skip over those 2 dwords in this case
            Ar.Position += 8;

            Ar.Position += stride;
            VertReuseBatchInfo = default;
        }

        if (
            Ar.Game >= EGame.GAME_UE5_0
            && page is not null
            && page.ClusterDiskHeaders.Length > clusterIndex
        )
        {
            FClusterDiskHeader clusterDiskHeader = page.ClusterDiskHeaders[clusterIndex];
            // read the material table
            if (ShouldUseMaterialTable())
            {
                MaterialRanges = new FMaterialRange[MaterialTableLength];
                Ar.Position = page.GPUPageHeaderOffset + MaterialTableOffset * 4;
                for (int i = 0; i < MaterialTableLength; i++)
                {
                    MaterialRanges[i] = new FMaterialRange(Ar.Read<uint>());
                }
            }

            // parse triangle indices
            TriIndices = new uint[NumTris][];
            for (uint triIndex = 0; triIndex < NumTris; triIndex++)
            {
                uint[] indices = GetTriangleIndices(Ar, page, clusterDiskHeader, clusterIndex, triIndex);
                if (indices[1] < Math.Min(indices[0], indices[2]))
                    (indices[0], indices[1], indices[2]) = (indices[1], indices[2], indices[0]);
                else if (indices[2] < Math.Min(indices[0], indices[1]))
                    (indices[0], indices[1], indices[2]) = (indices[2], indices[0], indices[1]);

                TriIndices[triIndex] = indices;
            }

            // identify vertex identification types
            GroupRefToVertex = new uint[clusterDiskHeader.NumVertexRefs];
            uint numNonRefVertices = NumVerts - clusterDiskHeader.NumVertexRefs;
            GroupNonRefToVertex = new uint[numNonRefVertices];
            uint[] groupNumRefsInPrevDwords8888 = [0, 0];
            long alignedBitmaskOffset = page.PageDiskHeaderOffset + page.PageDiskHeader.VertexRefBitmaskOffset + clusterIndex * 32; // NANITE_MAX_CLUSTER_VERTICES / 8
            for (uint groupIndex = 0; groupIndex < 7; groupIndex++)
            {
                Ar.Position = alignedBitmaskOffset + groupIndex * 4;
                uint count = (uint)BitOperations.PopCount(Ar.Read<uint>());
                uint count8888 = count * 0x01010101; // Broadcast count to all bytes
                uint index = groupIndex + 1;
                groupNumRefsInPrevDwords8888[index >> 2] += count8888 << (int) ((index & 3) << 3); // Add to bytes above
                if (NumVerts > 128 && index < 4)
                {
                    // Add low dword byte counts to all bytes in high dword when there are more than 128 vertices.
                    groupNumRefsInPrevDwords8888[1] += count8888;
                }
            }
            Vertices = new FNaniteVertex[NumVerts];
            for (uint vertexIndex = 0; vertexIndex < NumVerts; vertexIndex++)
            {
                Vertices[vertexIndex] = null;
                uint dwordIndex = vertexIndex >> 5;
                uint bitIndex = vertexIndex & 31;

                uint shift = (dwordIndex & 3) << 3;
                uint numRefsInPrevDwords = (groupNumRefsInPrevDwords8888[dwordIndex >> 2] >> (int)shift) & 0xFFu;
                Ar.Position = alignedBitmaskOffset + dwordIndex * 4;
                uint dwordMask = Ar.Read<uint>();
                uint numPrevRefVertices = (uint)BitOperations.PopCount(NaniteUtils.GetBits(dwordMask, (int) bitIndex, 0)) + numRefsInPrevDwords;

                if ((dwordMask & (1u << (int)bitIndex)) != 0u)
                {
                    GroupRefToVertex[numPrevRefVertices] = vertexIndex;
                }
                else
                {
                    uint numPrevNonRefVertices = vertexIndex - numPrevRefVertices;
                    GroupNonRefToVertex[numPrevNonRefVertices] = vertexIndex;
                }
            }

            // read non ref vert information
            for (int nonRefVertexIndex = 0; nonRefVertexIndex < numNonRefVertices; nonRefVertexIndex ++)
            {
                uint vertexIndex = GroupNonRefToVertex[nonRefVertexIndex];
                uint positionBitsPerVertex = PosBitsX + PosBitsY + PosBitsZ;
                uint srcPositionBitsPerVertex = (positionBitsPerVertex + 7) & ~7u;

                FNaniteVertex vertex = new FNaniteVertex() { IsRef = false };
                vertex.ReadPosData(
                    Ar,
                    page.PageDiskHeaderOffset + clusterDiskHeader.PositionDataOffset,
                    nonRefVertexIndex * srcPositionBitsPerVertex,
                    this
                );

                uint srcBitsPerAttribute = (BitsPerAttribute + 7) & ~7u;
                NaniteUtils.BitStreamReader reader = NaniteUtils.CreateBitStreamReader_Aligned(
                    page.PageDiskHeaderOffset + clusterDiskHeader.AttributeDataOffset,
                    nonRefVertexIndex * srcBitsPerAttribute,
                    GetMaxAttributeBits(Ar, NaniteUtils.NANITE_MAX_UVS)
                );
                vertex.ReadAttributeData(Ar, reader, page, this, clusterIndex);

                Vertices[vertexIndex] = vertex;
            }
        }
    }

    private static int GetMaxAttributeBits(FArchive Ar, int numTexCoords)
    {
        int ret =
            + 2 * NaniteUtils.NANITE_MAX_NORMAL_QUANTIZATION_BITS(Ar.Game)
            + 4 * NaniteUtils.NANITE_MAX_COLOR_QUANTIZATION_BITS
            + numTexCoords * (2 * NaniteUtils.NANITE_MAX_TEXCOORD_QUANTIZATION_BITS);
        if (Ar.Game >= EGame.GAME_UE5_3)
        {
            ret += 1 + NaniteUtils.NANITE_MAX_TANGENT_QUANTIZATION_BITS;
        }
        return ret;
    }

    /// <summary>Reads and unpacks the triangle indices of the cluster.</summary>
    private uint[] GetTriangleIndices(FArchive Ar, FNaniteStreamableData page, FClusterDiskHeader clusterDiskHeader, int clusterIndex, uint triIndex)
    {
        uint dwordIndex = triIndex >> 5;
        uint bitIndex = triIndex & 31;

        // Bitmask.x: bIsStart, Bitmask.y: bIsLeft, Bitmask.z: bIsNewVertex
        Ar.Position = page.PageDiskHeaderOffset + page.PageDiskHeader.StripBitmaskOffset + (clusterIndex * 4 + dwordIndex) * 12;
        uint sMask = Ar.Read<uint>();
        uint lMask = Ar.Read<uint>();
        uint wMask = Ar.Read<uint>();
        uint slMask = sMask & lMask;

        // const uint HeadRefVertexMask = ( SMask & LMask & WMask ) | ( ~SMask & WMask );
        uint headRefVertexMask = (slMask | ~sMask) & wMask; // 1 if head of triangle is ref. S case with 3 refs or L/R case with 1 ref.

        uint prevBitsMask = (1u << (int)bitIndex) - 1u;

        uint numPrevRefVerticesBeforeDword = dwordIndex == 0 ? 0u : NaniteUtils.GetBits(clusterDiskHeader.NumPrevRefVerticesBeforeDwords, 10, (int)(dwordIndex * 10 - 10));
        uint numPrevNewVerticesBeforeDword = dwordIndex == 0 ? 0u : NaniteUtils.GetBits(clusterDiskHeader.NumPrevNewVerticesBeforeDwords, 10, (int)(dwordIndex * 10 - 10));

        int currentDwordNumPrevRefVertices = (BitOperations.PopCount(slMask & prevBitsMask) << 1) + BitOperations.PopCount(wMask & prevBitsMask);
        int currentDwordNumPrevNewVertices = (BitOperations.PopCount(sMask & prevBitsMask) << 1) + (int)bitIndex - currentDwordNumPrevRefVertices;

        int numPrevRefVertices = (int)numPrevRefVerticesBeforeDword + currentDwordNumPrevRefVertices;
        int numPrevNewVertices = (int)numPrevNewVerticesBeforeDword + currentDwordNumPrevNewVertices;

        int isStart = NaniteUtils.GetBitsAsSigned(sMask, 1, (int) bitIndex); // -1: true, 0: false
        int isLeft = NaniteUtils.GetBitsAsSigned(lMask, 1, (int) bitIndex); // -1: true, 0: false
        int isRef = NaniteUtils.GetBitsAsSigned(wMask, 1, (int) bitIndex); // -1: true, 0: false

        // needs to allow underflow of u32
        uint baseVertex = unchecked((uint) (numPrevNewVertices - 1));

        uint[] outIndices = [ 0u, 0u, 0u ];
        long readBaseAddress = page.PageDiskHeaderOffset + clusterDiskHeader.IndexDataOffset;
        // -1 if not Start
        uint indexData = NaniteUtils.ReadUnalignedDword(Ar, readBaseAddress, (numPrevRefVertices + ~isStart) * 5);
        if (isStart != 0)
        {
            int minusNumRefVertices = (isLeft << 1) + isRef;
            uint nextVertex = unchecked((uint)numPrevNewVertices);

            if (minusNumRefVertices <= -1)
            {
                outIndices[0] = baseVertex - (indexData & 31);
                indexData >>= 5;
            }
            else
            {
                outIndices[0] = nextVertex++;
            }

            if (minusNumRefVertices <= -2)
            {
                outIndices[1] = baseVertex - (indexData & 31);
                indexData >>= 5;
            }
            else
            {
                outIndices[1] = nextVertex++;
            }

            if (minusNumRefVertices <= -3)
            {
                outIndices[2] = baseVertex - (indexData & 31);
            }
            else
            {
                outIndices[2] = nextVertex++;
            }
        }
        else
        {
            // Handle two first vertices
            uint prevBitIndex = bitIndex - 1u;
            int isPrevStart = NaniteUtils.GetBitsAsSigned(sMask, 1, (int)prevBitIndex);
            int isPrevHeadRef = NaniteUtils.GetBitsAsSigned(headRefVertexMask, 1, (int)prevBitIndex);
            //const int NumPrevNewVerticesInTriangle = IsPrevStart ? ( 3u - ( bfe_u32( /*SLMask*/ LMask, PrevBitIndex, 1 ) << 1 ) - bfe_u32( /*SMask &*/ WMask, PrevBitIndex, 1 ) ) : /*1u - IsPrevRefVertex*/ 0u;
            int numPrevNewVerticesInTriangle = isPrevStart & unchecked((int)(3u - ((NaniteUtils.GetBits( /*SLMask*/ lMask, 1, (int)prevBitIndex) << 1) | NaniteUtils.GetBits( /*SMask &*/ wMask, 1, (int)prevBitIndex))));

            //OutIndices[ 1 ] = IsPrevRefVertex ? ( BaseVertex - ( IndexData & 31u ) + NumPrevNewVerticesInTriangle ) : BaseVertex;	// BaseVertex = ( NumPrevNewVertices - 1 );
            outIndices[1] = (uint) (baseVertex + (isPrevHeadRef & (numPrevNewVerticesInTriangle - (indexData & 31u))));
            //OutIndices[ 2 ] = IsRefVertex ? ( BaseVertex - bfe_u32( IndexData, 5, 5 ) ) : NumPrevNewVertices;
            outIndices[2] = (uint) (numPrevNewVertices + (isRef & (-1 - NaniteUtils.GetBits(indexData, 5, 5))));

            // We have to search for the third vertex. 
            // Left triangles search for previous Right/Start. Right triangles search for previous Left/Start.
            uint searchMask = sMask | (lMask ^ unchecked((uint)isLeft));               // SMask | ( IsRight ? LMask : RMask );
            uint foundBitIndex = NaniteUtils.FirstBitHigh(searchMask & prevBitsMask);
            int isFoundCaseS = NaniteUtils.GetBitsAsSigned(sMask, 1, (int)foundBitIndex);       // -1: true, 0: false

            uint foundPrevBitsMask = unchecked((1u << unchecked((int)foundBitIndex)) - 1u);
            int foundCurrentDwordNumPrevRefVertices = (BitOperations.PopCount(slMask & foundPrevBitsMask) << 1) + BitOperations.PopCount(wMask & foundPrevBitsMask);
            int foundCurrentDwordNumPrevNewVertices = (BitOperations.PopCount(sMask & foundPrevBitsMask) << 1) + (int)foundBitIndex - foundCurrentDwordNumPrevRefVertices;

            int foundNumPrevNewVertices = (int)numPrevNewVerticesBeforeDword + foundCurrentDwordNumPrevNewVertices;
            int foundNumPrevRefVertices = (int)numPrevRefVerticesBeforeDword + foundCurrentDwordNumPrevRefVertices;

            uint foundNumRefVertices = (NaniteUtils.GetBits(lMask, 1, (int)foundBitIndex) << 1) + NaniteUtils.GetBits(wMask, 1, (int) foundBitIndex);
            uint isBeforeFoundRefVertex = NaniteUtils.GetBits(headRefVertexMask, 1, (int) foundBitIndex - 1);

            // ReadOffset: Where is the vertex relative to triangle we searched for?
            int readOffset = isFoundCaseS != 0 ? isLeft : 1;
            uint foundIndexData = NaniteUtils.ReadUnalignedDword(Ar, readBaseAddress, (foundNumPrevRefVertices - readOffset) * 5);
            uint foundIndex = ((uint)foundNumPrevNewVertices - 1u) - NaniteUtils.GetBits(foundIndexData, 5, 0);

            bool condition = isFoundCaseS != 0 ? ((int) foundNumRefVertices >= 1 - isLeft) : (isBeforeFoundRefVertex != 0);
            int foundNewVertex = foundNumPrevNewVertices + (isFoundCaseS != 0 ? (isLeft & (foundNumRefVertices == 0 ? 1 : 0)) : -1);
            outIndices[0] = condition ? foundIndex : (uint)foundNewVertex;

            if (isLeft != 0)
            {
                (outIndices[1], outIndices[2]) = (outIndices[2], outIndices[1]);
            }
        }


        return outIndices;
    }

    /// <summary>Gets the material index of a given triangle.</summary>
    /// <returns>The index of the material of the given triangle. uint.MAX_VALUE if not found.</returns>
    public uint GetMaterialIndex(int triangleIndex)
    {
        // slow path
        if (ShouldUseMaterialTable())
        {
            foreach (var range in MaterialRanges)
                if (triangleIndex >= range.TriStart && triangleIndex < (range.TriStart + range.TriLength))
                    return range.MaterialIndex;
            // default value if nothing is found
            return 0xFFFFFFFFu;
        }

        // fast path
        if (triangleIndex < Material0Length)
            return Material0Index;
        else if (triangleIndex < (Material0Length + Material1Length))
            return Material1Index;
        return Material2Index;
    }

    /// <summary>
    /// Checks if the cluster should reference the material table when identify material indices.
    /// </summary>
    /// <returns>True if the material table should be referenced instead.</returns>
    public bool ShouldUseMaterialTable()
    {
        return Material0Length == 0;
    }

    public void ResolveVertexReferences(FArchive Ar, FNaniteResources resources, FNaniteStreamableData page, FClusterDiskHeader clusterDiskHeader, FPageStreamingState pageStreamingState)
    {
        if (page.Clusters is null)
        {
            throw new ArgumentException($"{nameof(page)} hasn't loaded the cluster data yet!", nameof(page));
        }
        for (int refVertexIndex = 0; refVertexIndex < clusterDiskHeader.NumVertexRefs; refVertexIndex++) {

            uint vertexIndex = GroupRefToVertex[refVertexIndex];
            Ar.Position = page.PageDiskHeaderOffset + clusterDiskHeader.VertexRefDataOffset + refVertexIndex;
            byte pageClusterIndex = Ar.Read<byte>();

            Ar.Position = page.PageDiskHeaderOffset + clusterDiskHeader.PageClusterMapOffset + pageClusterIndex * 4;
            uint pageClusterData = Ar.Read<uint>();

            uint parentPageIndex = pageClusterData >> NaniteUtils.NANITE_MAX_CLUSTERS_PER_PAGE_BITS;
            uint srcLocalClusterIndex = NaniteUtils.GetBits(pageClusterData, NaniteUtils.NANITE_MAX_CLUSTERS_PER_PAGE_BITS, 0);
            Ar.Position = page.PageDiskHeaderOffset + clusterDiskHeader.VertexRefDataOffset + refVertexIndex + page.PageDiskHeader.NumVertexRefs;
            byte srcCodedVertexIndex = Ar.Read<byte>();

            FCluster srcCluster;
            uint parentGPUPageIndex = 0;

            bool isParentRef = parentPageIndex != 0;
            uint realSrcVertexIndex;
            if (isParentRef)
            {
                parentGPUPageIndex = resources.PageDependencies[pageStreamingState.DependenciesStart + (parentPageIndex - 1)];
                srcCluster = resources.GetPage((int)parentGPUPageIndex).Clusters[srcLocalClusterIndex];
                realSrcVertexIndex = srcCodedVertexIndex;
            }
            else
            {
                srcCluster = page.Clusters[srcLocalClusterIndex];
                realSrcVertexIndex = srcCluster.GroupNonRefToVertex[srcCodedVertexIndex];
            }

            // transcode position
            FNaniteVertex? srcVert = srcCluster.Vertices[realSrcVertexIndex];
            if (srcVert is null)
            {
                throw new InvalidOperationException("The source vertex doesn't appear to have been loaded yet.");
            }

            FIntVector newRawPos = srcVert.RawPos + srcCluster.PosStart - PosStart;
            Vertices[vertexIndex] = new FNaniteVertex()
            {
                Pos = (newRawPos + PosStart) * NaniteUtils.PrecisionScales[PosPrecision],
                RawPos = newRawPos,
                Attributes = srcVert.Attributes,
                IsRef = true
            };
        }
    }
}

/// <summary>A header that describes the contents of a nanite streaming page.</summary>
public readonly struct FPageDiskHeader
{
    /// <summary>The number of bytes that should follow the gpu header.</summary>
    public readonly uint GpuSize;
    /// <summary>The number of cluster present in this page.</summary>
    public readonly uint NumClusters;
    /// <summary>The bumber of uint4s used to store the cluster data, the material tables and the uv decode info.</summary>
    public readonly uint NumRawFloat4s;
    /// <summary>The number of UVs this cluster uses.</summary>
    public readonly uint NumTexCoords;
    /// <summary>The number of vertex references withing this page.</summary>
    public readonly uint NumVertexRefs;
    /// <summary>The offset from this header where the uv ranges can be found.</summary>
    public readonly uint DecodeInfoOffset;
    /// <summary>The offset from this header where the tirangle strip bitmasks start.</summary>
    public readonly uint StripBitmaskOffset;
    /// <summary>The offset from this header where the reference vertex bitmasks start.</summary>
    public readonly uint VertexRefBitmaskOffset;
}

/// <summary>A header that describes the cluster at the same index as itself.</summary>
public readonly struct FClusterDiskHeader
{
    /// <summary>The offset from the disk page header where the triangle indices for this cluster starts.</summary>
    public readonly uint IndexDataOffset;
    /// <summary>The offset from the disk page header the reference vertex page mapping for this cluster starts.</summary>
    public readonly uint PageClusterMapOffset;
    /// <summary>The offset from the disk page header the reference vertex data for this cluster starts.</summary>
    public readonly uint VertexRefDataOffset;
    /// <summary>The offset from the disk page header the non-ref vertex's positions for this cluster starts.</summary>
    public readonly uint PositionDataOffset;
    /// <summary>The offset from the disk page header the non-ref vertex's attributes for this cluster starts.</summary>
    public readonly uint AttributeDataOffset;
    /// <summary>The number of vertexes that are references in this cluster.</summary>
    public readonly uint NumVertexRefs;
    public readonly uint NumPrevRefVerticesBeforeDwords;
    public readonly uint NumPrevNewVerticesBeforeDwords;
}

/// <summary>A header that is directly transcoded to the gpu at runtime.</summary>
public readonly struct FPageGPUHeader
{
    /// <summary>The number of clusters in this page.</summary>
    public readonly uint NumClusters;
    [JsonIgnore]
    public readonly uint Pad1;
    [JsonIgnore]
    public readonly uint Pad2;
    [JsonIgnore]
    public readonly uint Pad3;
}

public class FNaniteStreamableData
{
    public FFixupChunk FixupChunk;

    /// <summary>Describes the contents of this page.</summary>
    public FPageDiskHeader PageDiskHeader;
    /// <summary>A list of headers that </summary>
    public FClusterDiskHeader[] ClusterDiskHeaders;
    // ignoring because they take up so much space in the JSON and aren't really usefull to the end user, would probably be nice to put behind a flag
    [JsonIgnore]
    public FPageGPUHeader PageGPUHeader;
    [JsonIgnore]
    public FCluster[] Clusters = [];
    [JsonIgnore]
    public FUVRange[][] UVRanges = [];

    // state variables used for parsing, this isn't serialized.
    [JsonIgnore]
    public long PageDiskHeaderOffset = -1;
    [JsonIgnore]
    public long GPUPageHeaderOffset = -1;

    public unsafe FNaniteStreamableData(FByteArchive Ar, FNaniteResources resources, int numRootPages, uint pageSize, int pageIndex)
    {
        FixupChunk = new FFixupChunk(Ar);

        // origin of all the offsets in the page cluster header
        PageDiskHeaderOffset = Ar.Position;
        PageDiskHeader = Ar.Read<FPageDiskHeader>();
        if (PageDiskHeader.NumClusters > NaniteUtils.NANITE_MAX_CLUSTERS_PER_PAGE)
        {
            throw new InvalidDataException($"Too many clusters in FNaniteStreamableData, {PageDiskHeader.NumClusters} max is {NaniteUtils.NANITE_MAX_CLUSTERS_PER_PAGE}");
        }
        if (PageDiskHeader.NumTexCoords > NaniteUtils.NANITE_MAX_UVS)
        {
            throw new InvalidDataException($"Too many tex coords in FNaniteStreamableData, {PageDiskHeader.NumTexCoords} max is {NaniteUtils.NANITE_MAX_UVS}");
        }
        if (PageDiskHeader.NumClusters != FixupChunk.Header.NumClusters)
        {
            throw new InvalidDataException($"Data corruption detected! page disk header and fixup cluster do not agree on the number of clusters. {PageDiskHeader.NumClusters} vs {FixupChunk.Header.NumClusters}");
        }
        ClusterDiskHeaders = Ar.ReadArray<FClusterDiskHeader>((int) PageDiskHeader.NumClusters);

        GPUPageHeaderOffset = Ar.Position;
        PageGPUHeader = Ar.Read<FPageGPUHeader>();
        if (PageGPUHeader.NumClusters != FixupChunk.Header.NumClusters)
        {
            throw new InvalidDataException($"Data corruption detected! page gpu header and fixup cluster do not agree on the number of clusters. {PageDiskHeader.NumClusters} vs {FixupChunk.Header.NumClusters}");
        }

        // Not stored as an array, it's actually stored as an SOA to speedup GPU transcoding
        Clusters = new FCluster[FixupChunk.Header.NumClusters];
        UVRanges = new FUVRange[FixupChunk.Header.NumClusters][];
        long clusterOrigin = Ar.Position;
        for (int clusterIndex = 0; clusterIndex < Clusters.Length; clusterIndex++)
        {
            // get the uv ranges
            UVRanges[clusterIndex] = new FUVRange[PageDiskHeader.NumTexCoords];
            Ar.Position = PageDiskHeaderOffset + PageDiskHeader.DecodeInfoOffset + clusterIndex * sizeof(FUVRange) * PageDiskHeader.NumTexCoords;
            UVRanges[clusterIndex] = Ar.ReadArray<FUVRange>((int)PageDiskHeader.NumTexCoords);

            Ar.Position = clusterOrigin + 16 * clusterIndex;
            Clusters[clusterIndex] = new FCluster(Ar, this, Clusters.Length, clusterIndex);
        }

        // resolve vertex references once all basic data is parsed
        for (int clusterIndex = 0; clusterIndex < Clusters.Length; clusterIndex++)
        {
            Clusters[clusterIndex].ResolveVertexReferences(Ar, resources, this, ClusterDiskHeaders[clusterIndex], resources.PageStreamingStates[pageIndex]);
        }
    }
}

public class FNaniteResources
{
    // Persistent State
    public FNaniteStreamableData[] RootData = []; // Root page is loaded on resource load, so we always have something to draw.
    public FByteBulkData? StreamablePages = null; // Remaining pages are streamed on demand.
    public ushort[] ImposterAtlas = [];
    public FPackedHierarchyNode[] HierarchyNodes = [];
    public uint[] HierarchyRootOffsets = [];
    public FPageStreamingState[] PageStreamingStates = [];
    public uint[] PageDependencies = [];
    public FMatrix3x4[] AssemblyTransforms = [];
    public FBoxSphereBounds? MeshBounds = null; // FBoxSphereBounds3f
    /// <summary>The number of root pages found outside of the bulk page.</summary>
    public int NumRootPages = 0;
    /// <summary>The precision which which vertex positions are recorded with.</summary>
    public int PositionPrecision = 0;
    /// <summary>The precision which which vertex normals are recorded with. Added with 5.2.</summary>
    public int NormalPrecision = 0;
    /// <summary>The number of triangles the original mesh had.</summary>
    public uint NumInputTriangles = 0;
    /// <summary>The number of verticies the original mesh had.</summary>
    public uint NumInputVertices = 0;
    public ushort NumInputMeshes = 0;
    /// <summary>The number of UVs used by the origina mesh.</summary>
    public ushort NumInputTexCoords = 0;
    /// <summary>The number of clusters in total for this mesh.</summary>
    public uint NumClusters = 0;
    public uint ResourceFlags = 0;

    [JsonIgnore]
    public FNaniteStreamableData?[] LoadedPages = [];
    [JsonIgnore]
    public readonly VersionContainer TemplateArchiveVersion;
    [JsonIgnore]
    private byte[] RootPages = [];

    public FNaniteResources(FAssetArchive Ar)
    {
        TemplateArchiveVersion = Ar.Versions;

        var stripFlags = new FStripDataFlags(Ar);
        if (!stripFlags.IsAudioVisualDataStripped())
        {
            ResourceFlags = Ar.Read<uint>();
            StreamablePages = new FByteBulkData(Ar);
            RootPages = Ar.ReadArray<byte>();
            PageStreamingStates = Ar.ReadArray(() => new FPageStreamingState(Ar));
            var count = Ar.Read<uint>();
            HierarchyNodes = new FPackedHierarchyNode[count];
            for (uint i = 0; i < count; i++)
            {
                HierarchyNodes[i] = new FPackedHierarchyNode(Ar, i);
            }
            HierarchyRootOffsets = Ar.ReadArray<uint>();
            PageDependencies = Ar.ReadArray<uint>();
            if (Ar.Game >= EGame.GAME_UE5_6)
            {
                AssemblyTransforms = Ar.ReadArray<FMatrix3x4>();
                MeshBounds = new FBoxSphereBounds(Ar.Read<FVector>(), Ar.Read<FVector>(), Ar.Read<float>());
            }
            ImposterAtlas = Ar.ReadArray<ushort>();
            NumRootPages = Ar.Read<int>();
            PositionPrecision = Ar.Read<int>();
            if (Ar.Game >= EGame.GAME_UE5_2) NormalPrecision = Ar.Read<int>();
            NumInputTriangles = Ar.Read<uint>();
            NumInputVertices = Ar.Read<uint>();
            if (Ar.Game < EGame.GAME_UE5_6)
            {
                NumInputMeshes = Ar.Read<ushort>();
                NumInputTexCoords = Ar.Read<ushort>();
            }
            if (Ar.Game >= EGame.GAME_UE5_1) NumClusters = Ar.Read<uint>();

            if (PageStreamingStates.Length > 0)
            {
                LoadedPages = new FNaniteStreamableData[PageStreamingStates.Length];
                RootData = new FNaniteStreamableData[NumRootPages];

                // the format does allow for more than one
                for (int pageIndex = 0; pageIndex < NumRootPages; pageIndex++)
                {
                    RootData[pageIndex] = GetPage(pageIndex);
                }
            }
        }
    }

    public void LoadBulkPages()
    {
        // also try to load the non bulk just in case, it's not like it's gonna double load them anyway.
        for (int pageIndex = 0; pageIndex < PageStreamingStates.Length; pageIndex++)
        {
            GetPage(pageIndex);
        }
    }

    public FNaniteStreamableData GetPage(int pageIndex)
    {
        if (LoadedPages == null)
        {
            throw new InvalidOperationException("Cannot parse page information, does your game support nanite?");
        }
        if (pageIndex < 0 || pageIndex >= LoadedPages.Length)
        {
            throw new ArgumentOutOfRangeException(nameof(pageIndex), $"0 is out of range! {NumRootPages} <= {pageIndex} < {LoadedPages.Length}");
        }
        if (LoadedPages[pageIndex] == null)
        {
            LoadedPages[pageIndex] = LoadPage(pageIndex);
        }
        return LoadedPages[pageIndex]!;
    }

    private FNaniteStreamableData LoadPage(int pageIndex)
    {
        FPageStreamingState page = PageStreamingStates[pageIndex];
        byte[] buffer;
        if (pageIndex < NumRootPages)
        {
            buffer = RootPages[(int) page.BulkOffset..(int) (page.BulkOffset + page.BulkSize)];
        }
        else
        {
            if (StreamablePages?.Data is null)
            {
                throw new InvalidOperationException("Tried to read bulk page when bulk data is empty!");
            }
            buffer = StreamablePages.Data[(int) page.BulkOffset..(int) (page.BulkOffset + page.BulkSize)];
        }
        var pageArchive = new FByteArchive($"NaniteStreamablePage{pageIndex}", buffer, TemplateArchiveVersion);
        return new FNaniteStreamableData(pageArchive, this, NumRootPages, PageStreamingStates[0].PageSize, pageIndex);
    }
}
