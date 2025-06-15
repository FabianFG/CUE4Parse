using System;
using System.Numerics;
using CUE4Parse.UE4.Objects.Core.Math;
using CUE4Parse.UE4.Readers;
using CUE4Parse.UE4.Versions;
using static CUE4Parse.UE4.Assets.Exports.Nanite.NaniteConstants;
using static CUE4Parse.UE4.Assets.Exports.Nanite.NaniteUtils;

namespace CUE4Parse.UE4.Assets.Exports.Nanite;

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
    public TIntVector4<uint> ColorMin;
    /// <summary>number of bits for each channel of the vertex colors, each taking 4 bits.</summary>
    public uint ColorBits;
    public TIntVector4<int> ColorComponentBits;
    /// <summary>Debug value, never actually used.</summary>
    public uint GroupIndex;
    /// <summary>The "starting point" to which all vertices in this cluster are relative to.</summary>
    public FIntVector PosStart;
    /// <summary>The number of bits used per vertex index in the strip indices when the strip are optimized at runtime. Unused for our purposes.</summary>
    public uint BitsPerIndex;
    /// <summary>A multiplier used to scale the delta between the pos start and the vertex positions.</summary>
    public int PosPrecision;
    public float PosScale;
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
    public NANITE_CLUSTER_FLAG Flags;
    public uint NumClusterBoneInfluences;
    /// <summary>The offset from the gpu page header where the vertex attributes of non-ref vertices can be found.</summary>
    public uint AttributeOffset;
    /// <summary>The number of bits used to store the atribute data for a non-ref vertex in this cluster.</summary>
    public uint BitsPerAttribute;
    /// <summary>The offset from the gpu page header where the uv ranges for this cluster can be found.</summary>
    public uint DecodeInfoOffset;
    /// <summary>True if the mesh has explicit tangents, only available after 5.3</summary>
    public bool bHasTangents;
    public bool bSkinning;
    /// <summary>The number of uv ranges associated with this cluster.</summary>
    public uint NumUVs;
    public uint ColorMode;
    /// <summary>A map of the number of bits which which uv positions are serialized as. each byte can be separated into 2 nibbles, each representing the x and y coordinates.</summary>
    public uint UVBitOffsets;
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

    public uint ExtendedDataOffset;
    public uint ExtendedDataNum;
    public uint BrickDataOffset;
    public uint BrickDataNum;

    // decoded mesh data
    public FNaniteVertex?[] Vertices = [];
    public FUIntVector[] TriIndices = [];
    public FMaterialRange[] MaterialRanges = [];
    public uint[] GroupRefToVertex = [];
    public uint[] GroupNonRefToVertex = [];
    public FUVRange_Old[] UVRanges_Old = [];
    public FUVRange[] UVRanges = [];

    public FCluster(FArchive Ar, int stride)
    {
        var numVerts_positionOffset = Ar.Read<uint>();
        if (Ar.Game >= EGame.GAME_UE5_6)
        {
            NumVerts = GetBits(numVerts_positionOffset, 14, 0);
            PositionOffset = GetBits(numVerts_positionOffset, 18, 14);
        }
        else
        {
            NumVerts = GetBits(numVerts_positionOffset, 9, 0);
            PositionOffset = GetBits(numVerts_positionOffset, 23, 9);
        }

        var numTris_indexOffset = Ar.Read<uint>();
        NumTris = GetBits(numTris_indexOffset, 8, 0);
        IndexOffset = GetBits(numTris_indexOffset, 24, 8);

        var colorMin = Ar.Read<uint>();
        ColorMin = new TIntVector4<uint>(UnpackByte0(colorMin), UnpackByte1(colorMin), UnpackByte2(colorMin), UnpackByte3(colorMin));

        var colorBits_groupIndex = Ar.Read<uint>();
        ColorBits = GetBits(colorBits_groupIndex, 16, 0);
        GroupIndex = GetBits(colorBits_groupIndex, 16, 16); // debug only

        ColorComponentBits = new TIntVector4<int>(
            (int) GetBits(ColorBits, 4, 0), (int) GetBits(ColorBits, 4, 4),
            (int) GetBits(ColorBits, 4, 8), (int) GetBits(ColorBits, 4, 12)
            );

        Ar.Position += stride;
        PosStart = Ar.Read<FIntVector>();

        var bitsPerIndex_posPrecision_posBits = Ar.Read<uint>();
        if (Ar.Game >= EGame.GAME_UE5_4)
        {
            BitsPerIndex = GetBits(bitsPerIndex_posPrecision_posBits, 3, 0) + 1;
            PosPrecision = (int) GetBits(bitsPerIndex_posPrecision_posBits, 6, 3) + NANITE_MIN_POSITION_PRECISION_504;
        }
        else
        {
            BitsPerIndex = GetBits(bitsPerIndex_posPrecision_posBits, 4, 0);
            PosPrecision = (int) GetBits(bitsPerIndex_posPrecision_posBits, 5, 4) + NANITE_MIN_POSITION_PRECISION_500;
        }
        PosBitsX = GetBits(bitsPerIndex_posPrecision_posBits, 5, 9);
        PosBitsY = GetBits(bitsPerIndex_posPrecision_posBits, 5, 14);
        PosBitsZ = GetBits(bitsPerIndex_posPrecision_posBits, 5, 19);
        NormalPrecision = Ar.Game >= EGame.GAME_UE5_2 ? GetBits(bitsPerIndex_posPrecision_posBits, 4, 24) : NANITE_MAX_NORMAL_QUANTIZATION_BITS_500;
        TangentPrecision = Ar.Game >= EGame.GAME_UE5_3 ? GetBits(bitsPerIndex_posPrecision_posBits, 4, 28) : 0;
        PosScale = PrecisionScales[PosPrecision];

        Ar.Position += stride;
        LODBounds = Ar.Read<FVector4>();

        Ar.Position += stride;
        BoxBoundsCenter = Ar.Read<FVector>();
        LODError = (float) Ar.Read<Half>();
        EdgeLength = (float) Ar.Read<Half>();

        Ar.Position += stride;
        BoxBoundsExtent = Ar.Read<FVector>();
        if (Ar.Game >= EGame.GAME_UE5_6)
        {
            var packed = Ar.Read<uint>();
            Flags = (NANITE_CLUSTER_FLAG) GetBits(packed, 4, 0);
            NumClusterBoneInfluences = GetBits(packed, 5, 4);
        }
        else
        {
            Flags = Ar.Read<NANITE_CLUSTER_FLAG>();
        }

        Ar.Position += stride;
        var attributeOffset_bitsPerAttribute = Ar.Read<uint>();
        AttributeOffset = GetBits(attributeOffset_bitsPerAttribute, 22, 0);
        BitsPerAttribute = GetBits(attributeOffset_bitsPerAttribute, 10, 22);

        var decodeInfoOffset_bHasTengants_numUVs_colorMode = Ar.Read<uint>();
        DecodeInfoOffset = NaniteUtils.GetBits(decodeInfoOffset_bHasTengants_numUVs_colorMode, 22, 0);
        if (Ar.Game >= EGame.GAME_UE5_5)
        {
            bHasTangents = GetBits(decodeInfoOffset_bHasTengants_numUVs_colorMode, 1, 22) == 1;
            bSkinning = GetBits(decodeInfoOffset_bHasTengants_numUVs_colorMode, 1, 23) == 1;
            NumUVs = GetBits(decodeInfoOffset_bHasTengants_numUVs_colorMode, 3, 24);
            ColorMode = GetBits(decodeInfoOffset_bHasTengants_numUVs_colorMode, 2, 27);
        }
        if (Ar.Game >= EGame.GAME_UE5_3)
        {
            bHasTangents = GetBits(decodeInfoOffset_bHasTengants_numUVs_colorMode, 1, 22) == 1;
            NumUVs = GetBits(decodeInfoOffset_bHasTengants_numUVs_colorMode, 3, 23);
            ColorMode = GetBits(decodeInfoOffset_bHasTengants_numUVs_colorMode, 2, 26);
        }
        else
        {
            NumUVs = GetBits(decodeInfoOffset_bHasTengants_numUVs_colorMode, 3, 22);
            ColorMode = GetBits(decodeInfoOffset_bHasTengants_numUVs_colorMode, 2, 25);
        }

        UVBitOffsets = Ar.Read<uint>();
        var materialEncoding = Ar.Read<uint>();
        Ar.Position += stride;
        if (Ar.Game >= EGame.GAME_UE5_4)
        {
            var ExtendedDataOfsset_Num = Ar.Read<uint>();
            ExtendedDataOffset = GetBits(ExtendedDataOfsset_Num, 22, 0);
            ExtendedDataNum = GetBits(ExtendedDataOfsset_Num, 10, 22);
            var BrickDataOfsset_Num = Ar.Read<uint>();
            BrickDataOffset = GetBits(BrickDataOfsset_Num, 22, 0);
            BrickDataNum = GetBits(BrickDataOfsset_Num, 10, 22);
            Ar.Position += 8;
            Ar.Position += stride;
        }

        if (materialEncoding < 0xFE000000u)
        {
            // fast path
            MaterialTableOffset = 0;
            MaterialTableLength	= 0;
            Material0Index = GetBits(materialEncoding, 6, 0);
            Material1Index = GetBits(materialEncoding, 6, 6);
            Material2Index = GetBits(materialEncoding, 6, 12);
            Material0Length = GetBits(materialEncoding, 7, 18) + 1;
            Material1Length = GetBits(materialEncoding, 7, 25);
            VertReuseBatchCountTableOffset = 0;
            VertReuseBatchCountTableSize = 0;

            Ar.Position += stride;
            VertReuseBatchInfo = Ar.Game >= EGame.GAME_UE5_1 ? Ar.Read<TIntVector4<uint>>() : default;
            MaterialRanges = [];
        }
        else
        {
            // slow path
            MaterialTableOffset = GetBits(materialEncoding, 19, 0);
            MaterialTableLength = GetBits(materialEncoding, 6, 19) + 1;
            Material0Index = 0;
            Material1Index = 0;
            Material2Index = 0;
            Material0Length = 0;
            Material1Length = 0;
            Material1Length = 0;
            if (Ar.Game >= EGame.GAME_UE5_1)
            {
                VertReuseBatchCountTableOffset = Ar.Read<uint>();
                VertReuseBatchCountTableSize = Ar.Read<uint>();
                Ar.Position += 8;
            }
            VertReuseBatchInfo = default;
        }
    }

    public void Decode(FArchive Ar, FNaniteStreamableData? page, uint clusterIndex)
    {
        FClusterDiskHeader clusterDiskHeader = page.ClusterDiskHeaders[clusterIndex];
        // read the material table
        if (ShouldUseMaterialTable())
        {
            Ar.Position = page.GPUPageHeaderOffset + MaterialTableOffset * 4;
            MaterialRanges = Ar.ReadArray((int) MaterialTableLength, () => new FMaterialRange(Ar.Read<uint>()));
        }

        // parse triangle indices
        TriIndices = new FUIntVector[NumTris];
        for (uint triIndex = 0; triIndex < NumTris; triIndex++)
        {
            (uint x, uint y, uint z) = GetTriangleIndices(Ar, page, clusterDiskHeader, clusterIndex, triIndex);
            if (y < Math.Min(x, z))
                (x, y, z) = (y, z, x);
            else if (z < Math.Min(x, y))
                (x, y, z) = (z, x, y);

            TriIndices[triIndex] = new FUIntVector(x, y, z);
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
            uint dwordIndex = vertexIndex >> 5;
            uint bitIndex = vertexIndex & 31;

            uint shift = (dwordIndex & 3) << 3;
            uint numRefsInPrevDwords = (groupNumRefsInPrevDwords8888[dwordIndex >> 2] >> (int)shift) & 0xFFu;
            Ar.Position = alignedBitmaskOffset + dwordIndex * 4;
            uint dwordMask = Ar.Read<uint>();
            uint numPrevRefVertices = (uint)BitOperations.PopCount(GetBits(dwordMask, (int) bitIndex, 0)) + numRefsInPrevDwords;

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

        Ar.Position = page.GPUPageHeaderOffset + DecodeInfoOffset;
        if (Ar.Game >= EGame.GAME_UE5_4)
        {
            UVRanges = Ar.ReadArray((int) NumUVs, () => new FUVRange(Ar));
        }
        else
        {
            UVRanges_Old = Ar.ReadArray<FUVRange_Old>((int) NumUVs);
        }

        // read non ref vert information
        for (int nonRefVertexIndex = 0; nonRefVertexIndex < numNonRefVertices; nonRefVertexIndex ++)
        {
            uint vertexIndex = GroupNonRefToVertex[nonRefVertexIndex];
            uint positionBitsPerVertex = PosBitsX + PosBitsY + PosBitsZ;
            uint srcPositionBitsPerVertex = (positionBitsPerVertex + 7) & ~7u;

            var vertex = new FNaniteVertex();
            vertex.ReadPosData(
                Ar,
                page.PageDiskHeaderOffset + clusterDiskHeader.PositionDataOffset,
                nonRefVertexIndex * srcPositionBitsPerVertex,
                this
            );

            uint srcBitsPerAttribute = (BitsPerAttribute + 7) & ~7u;
            BitStreamReader reader = CreateBitStreamReader_Aligned(
                page.PageDiskHeaderOffset + clusterDiskHeader.AttributeDataOffset,
                nonRefVertexIndex * srcBitsPerAttribute,
                GetMaxAttributeBits(Ar, NANITE_MAX_UVS)
            );
            vertex.ReadAttributeData(Ar, reader, this);

            Vertices[vertexIndex] = vertex;
        }
    }

    private static int GetMaxAttributeBits(FArchive Ar, int numTexCoords)
    {
        int ret =
            + 2 * NANITE_MAX_NORMAL_QUANTIZATION_BITS(Ar.Game)
            + 4 * NANITE_MAX_COLOR_QUANTIZATION_BITS
            + numTexCoords * (2 * NANITE_MAX_TEXCOORD_QUANTIZATION_BITS);
        // To-Do: NANITE_MAX_TEXCOORD_QUANTIZATION_BITS in 5.4+
        if (Ar.Game >= EGame.GAME_UE5_3)
        {
            ret += 1 + NANITE_MAX_TANGENT_QUANTIZATION_BITS;
        }
        return ret;
    }

    /// <summary>Reads and unpacks the triangle indices of the cluster.</summary>
    private (uint, uint, uint) GetTriangleIndices(FArchive Ar, FNaniteStreamableData page, FClusterDiskHeader clusterDiskHeader, uint clusterIndex, uint triIndex)
    {
        uint dwordIndex = triIndex >> 5;
        uint bitIndex = triIndex & 31;

        // Bitmask.x: bIsStart, Bitmask.y: bIsLeft, Bitmask.z: bIsNewVertex
        Ar.Position = page.PageDiskHeaderOffset + page.PageDiskHeader.StripBitmaskOffset + (clusterIndex * 4 + dwordIndex) * 12;
        var stripBitmasks = Ar.Read<TIntVector3<uint>>();
        uint sMask = stripBitmasks.X;
        uint lMask = stripBitmasks.Y;
        uint wMask = stripBitmasks.Z;
        uint slMask = sMask & lMask;

        // const uint HeadRefVertexMask = ( SMask & LMask & WMask ) | ( ~SMask & WMask );
        uint headRefVertexMask = (slMask | ~sMask) & wMask; // 1 if head of triangle is ref. S case with 3 refs or L/R case with 1 ref.

        uint prevBitsMask = (1u << (int)bitIndex) - 1u;

        uint numPrevRefVerticesBeforeDword = dwordIndex == 0 ? 0u : GetBits(clusterDiskHeader.NumPrevRefVerticesBeforeDwords, 10, (int)(dwordIndex * 10 - 10));
        uint numPrevNewVerticesBeforeDword = dwordIndex == 0 ? 0u : GetBits(clusterDiskHeader.NumPrevNewVerticesBeforeDwords, 10, (int)(dwordIndex * 10 - 10));

        int currentDwordNumPrevRefVertices = (BitOperations.PopCount(slMask & prevBitsMask) << 1) + BitOperations.PopCount(wMask & prevBitsMask);
        int currentDwordNumPrevNewVertices = (BitOperations.PopCount(sMask & prevBitsMask) << 1) + (int)bitIndex - currentDwordNumPrevRefVertices;

        int numPrevRefVertices = (int)numPrevRefVerticesBeforeDword + currentDwordNumPrevRefVertices;
        int numPrevNewVertices = (int)numPrevNewVerticesBeforeDword + currentDwordNumPrevNewVertices;

        int isStart = GetBitsAsSigned(sMask, 1, (int) bitIndex); // -1: true, 0: false
        int isLeft = GetBitsAsSigned(lMask, 1, (int) bitIndex); // -1: true, 0: false
        int isRef = GetBitsAsSigned(wMask, 1, (int) bitIndex); // -1: true, 0: false

        // needs to allow underflow of u32
        uint baseVertex = unchecked((uint) (numPrevNewVertices - 1));

        (uint x, uint y, uint z) = (0, 0, 0);
        long readBaseAddress = page.PageDiskHeaderOffset + clusterDiskHeader.IndexDataOffset;
        // -1 if not Start
        uint indexData = NaniteUtils.ReadUnalignedDword(Ar, readBaseAddress, (numPrevRefVertices + ~isStart) * 5);
        if (isStart != 0)
        {
            int minusNumRefVertices = (isLeft << 1) + isRef;
            uint nextVertex = unchecked((uint)numPrevNewVertices);

            if (minusNumRefVertices <= -1)
            {
                x = baseVertex - (indexData & 31);
                indexData >>= 5;
            }
            else
            {
                x = nextVertex++;
            }

            if (minusNumRefVertices <= -2)
            {
                y = baseVertex - (indexData & 31);
                indexData >>= 5;
            }
            else
            {
                y = nextVertex++;
            }

            if (minusNumRefVertices <= -3)
            {
                z = baseVertex - (indexData & 31);
            }
            else
            {
                z = nextVertex++;
            }
        }
        else
        {
            // Handle two first vertices
            uint prevBitIndex = bitIndex - 1u;
            int isPrevStart = GetBitsAsSigned(sMask, 1, (int)prevBitIndex);
            int isPrevHeadRef = GetBitsAsSigned(headRefVertexMask, 1, (int)prevBitIndex);
            //const int NumPrevNewVerticesInTriangle = IsPrevStart ? ( 3u - ( bfe_u32( /*SLMask*/ LMask, PrevBitIndex, 1 ) << 1 ) - bfe_u32( /*SMask &*/ WMask, PrevBitIndex, 1 ) ) : /*1u - IsPrevRefVertex*/ 0u;
            int numPrevNewVerticesInTriangle = isPrevStart & unchecked((int)(3u - ((GetBits( /*SLMask*/ lMask, 1, (int)prevBitIndex) << 1) | GetBits( /*SMask &*/ wMask, 1, (int)prevBitIndex))));

            //OutIndices[ 1 ] = IsPrevRefVertex ? ( BaseVertex - ( IndexData & 31u ) + NumPrevNewVerticesInTriangle ) : BaseVertex;	// BaseVertex = ( NumPrevNewVertices - 1 );
            y = (uint) (baseVertex + (isPrevHeadRef & (numPrevNewVerticesInTriangle - (indexData & 31u))));
            //OutIndices[ 2 ] = IsRefVertex ? ( BaseVertex - bfe_u32( IndexData, 5, 5 ) ) : NumPrevNewVertices;
            z = (uint) (numPrevNewVertices + (isRef & (-1 - GetBits(indexData, 5, 5))));

            // We have to search for the third vertex.
            // Left triangles search for previous Right/Start. Right triangles search for previous Left/Start.
            uint searchMask = sMask | (lMask ^ unchecked((uint)isLeft));               // SMask | ( IsRight ? LMask : RMask );
            uint foundBitIndex = FirstBitHigh(searchMask & prevBitsMask);
            int isFoundCaseS = GetBitsAsSigned(sMask, 1, (int)foundBitIndex);       // -1: true, 0: false

            uint foundPrevBitsMask = unchecked((1u << unchecked((int)foundBitIndex)) - 1u);
            int foundCurrentDwordNumPrevRefVertices = (BitOperations.PopCount(slMask & foundPrevBitsMask) << 1) + BitOperations.PopCount(wMask & foundPrevBitsMask);
            int foundCurrentDwordNumPrevNewVertices = (BitOperations.PopCount(sMask & foundPrevBitsMask) << 1) + (int)foundBitIndex - foundCurrentDwordNumPrevRefVertices;

            int foundNumPrevNewVertices = (int)numPrevNewVerticesBeforeDword + foundCurrentDwordNumPrevNewVertices;
            int foundNumPrevRefVertices = (int)numPrevRefVerticesBeforeDword + foundCurrentDwordNumPrevRefVertices;

            uint foundNumRefVertices = (GetBits(lMask, 1, (int)foundBitIndex) << 1) + GetBits(wMask, 1, (int) foundBitIndex);
            uint isBeforeFoundRefVertex = GetBits(headRefVertexMask, 1, (int) foundBitIndex - 1);

            // ReadOffset: Where is the vertex relative to triangle we searched for?
            int readOffset = isFoundCaseS != 0 ? isLeft : 1;
            uint foundIndexData = NaniteUtils.ReadUnalignedDword(Ar, readBaseAddress, (foundNumPrevRefVertices - readOffset) * 5);
            uint foundIndex = ((uint)foundNumPrevNewVertices - 1u) - NaniteUtils.GetBits(foundIndexData, 5, 0);

            bool condition = isFoundCaseS != 0 ? ((int) foundNumRefVertices >= 1 - isLeft) : (isBeforeFoundRefVertex != 0);
            int foundNewVertex = foundNumPrevNewVertices + (isFoundCaseS != 0 ? (isLeft & (foundNumRefVertices == 0 ? 1 : 0)) : -1);
            x = condition ? foundIndex : (uint)foundNewVertex;

            if (isLeft != 0)
            {
                (y, z) = (z, y);
            }
        }

        return (x, y, z);
    }

    /// <summary>Gets the material index of a given triangle.</summary>
    /// <returns>The index of the material of the given triangle. uint.MAX_VALUE if not found.</returns>
    public uint GetMaterialIndex(uint triangleIndex)
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
    public bool ShouldUseMaterialTable() => Material0Length == 0;

    public void ResolveVertexReferences(FArchive Ar, FNaniteResources resources, FNaniteStreamableData page, FClusterDiskHeader clusterDiskHeader, FPageStreamingState pageStreamingState)
    {
        for (int refVertexIndex = 0; refVertexIndex < clusterDiskHeader.NumVertexRefs; refVertexIndex++)
        {
            uint vertexIndex = GroupRefToVertex[refVertexIndex];
            Ar.Position = page.PageDiskHeaderOffset + clusterDiskHeader.VertexRefDataOffset + refVertexIndex;
            byte pageClusterIndex = Ar.Read<byte>();

            Ar.Position = page.PageDiskHeaderOffset + clusterDiskHeader.PageClusterMapOffset + pageClusterIndex * 4;
            uint pageClusterData = Ar.Read<uint>();

            uint parentPageIndex = pageClusterData >> NANITE_MAX_CLUSTERS_PER_PAGE_BITS;
            uint srcLocalClusterIndex = GetBits(pageClusterData, NANITE_MAX_CLUSTERS_PER_PAGE_BITS, 0);
            Ar.Position = page.PageDiskHeaderOffset + clusterDiskHeader.VertexRefDataOffset + refVertexIndex + page.PageDiskHeader.NumVertexRefs;
            byte srcCodedVertexIndex = Ar.Read<byte>();

            FCluster srcCluster;
            uint parentGPUPageIndex = 0;

            bool isParentRef = parentPageIndex != 0;
            uint realSrcVertexIndex;
            if (isParentRef)
            {
                parentGPUPageIndex = resources.PageDependencies[pageStreamingState.DependenciesStart + (parentPageIndex - 1)];
                srcCluster = resources.GetPage(parentGPUPageIndex).Clusters[srcLocalClusterIndex];
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
                Pos = (newRawPos + PosStart) * PrecisionScales[PosPrecision],
                RawPos = newRawPos,
                Attributes = srcVert.Attributes,
                IsRef = true
            };
        }
    }
}
