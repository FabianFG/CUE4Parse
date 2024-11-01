using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Readers;

namespace CUE4Parse.UE4.Assets.Exports.CustomizableObject.Mutable.Meshes;

public class FMeshBufferChannel
{
    public EMeshBufferSemantic Semantic;
    public EMeshBufferFormat Format;
    public int SemanticIndex;
    public ushort Offset;
    public ushort ComponentCount;

    public FMeshBufferChannel(FArchive Ar)
    {
        Semantic = Ar.Read<EMeshBufferSemantic>();
        Format = Ar.Read<EMeshBufferFormat>();
        SemanticIndex = Ar.Read<int>();
        Offset = Ar.Read<ushort>();
        ComponentCount = Ar.Read<ushort>();
    }
}

public enum EMeshBufferSemantic : uint
{
    None,

    /** For index buffers, and mesh morphs */
    VertexIndex,

    /** Standard vertex semantics */
    Position,
    Normal,
    Tangent,
    Binormal,
    TexCoords,
    Color,
    BoneWeights,
    BoneIndices,

    /**
     * Internal semantic indicating what layout block each vertex belongs to.
     * It can be safely ignored if present in meshes returned by the system.
     * It will never be in the same buffer that other vertex semantics.
     */
    LayoutBlock,

    _DEPRECATED,

    /**
     * To let users define channels with semantics unknown to the system.
     * These channels will never be transformed, and the per-vertex or per-index data will be
     * simply copied.
     */
    Other,

    _DEPRECATED2,

    /** Semantics usefule for mesh binding. */
    TriangleIndex,
    BarycentricCoords,
    Distance,

    /** Semantics useful for alternative skin weight profiles. */
    AltSkinWeight,

    /** Utility */
    Count,
}

public enum EMeshBufferFormat
{
    None,

    Float16,
    Float32,

    UInt8,
    UInt16,
    UInt32,
    Int8,
    Int16,
    Int32,

    /** Integers interpreted as being in the range 0.0f to 1.0f */
    NUInt8,
    NUInt16,
    NUInt32,

    /** Integers interpreted as being in the range -1.0f to 1.0f */
    NInt8,
    NInt16,
    NInt32,

    /** Packed 1 to -1 value using multiply+add (128 is almost zero). Use 8-bit unsigned ints. */
    PackedDir8,

    /**
     * Same as EMeshBufferFormat::PackedDir8, with the w component replaced with the sign of the determinant
     * of the vertex basis to define the orientation of the tangent space in UE4 format.
     * Use 8-bit unsigned ints.
    */
    PackedDir8_W_TangentSign,

    /** Packed 1 to -1 value using multiply+add (128 is almost zero). Use 8-bit signed ints. */
    PackedDirS8,

    /**
     * Same as EMeshBufferFormat::PackedDirS8, with the w component replaced with the sign of the determinant
     * of the vertex basis to define the orientation of the tangent space in UE4 format.
     * Use 8-bit signed ints.
     */
    PackedDirS8_W_TangentSign,

    Float64,
    UInt64,
    Int64,
    NUInt64,
    NInt64,

    Count,
}