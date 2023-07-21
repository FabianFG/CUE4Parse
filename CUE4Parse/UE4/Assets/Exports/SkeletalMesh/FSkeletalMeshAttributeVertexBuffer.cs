using CUE4Parse.UE4.Assets.Exports.Texture;
using CUE4Parse.UE4.Readers;

namespace CUE4Parse.UE4.Assets.Exports.SkeletalMesh
{
    public enum ESkeletalMeshVertexAttributeDataType
    {
        // Store the vertex attribute values as a 32-bit floating point (float)
        Float,

        // Store the vertex attribute values as a 16-bit floating point (half)
        HalfFloat,

        // Quantize and store the vertex attribute values as an unsigned normalized 8-bit integer. Values outside the [0.0 - 1.0] range are clamped.
        NUInt8,

        // Commented out until we have PixelFormat support for these types.
        // NSInt8 UMETA(DisplayName="8-bit Signed Normalized", ToolTip="Quantize and store the vertex attribute values as a signed normalized 8-bit integer. Values outside the [-1.0 - 1.0] range are clamped."),
        // NUInt16 UMETA(DisplayName="16-bit Unsigned Normalized", ToolTip="Quantize and store the vertex attribute values as an unsigned normalized 16-bit integer. Values outside the [0.0 - 1.0] range are clamped."),
        // NSInt16 UMETA(DisplayName="16-bit Signed Normalized", ToolTip="Quantize and store the vertex attribute values as a signed normalized 16-bit integer. Values outside the [-1.0 - 1.0] range are clamped.")
    }

    public class FSkeletalMeshVertexAttributeData
    {
        public readonly int Stride;

        public FSkeletalMeshVertexAttributeData(FArchive Ar)
        {
            Stride = Ar.Read<int>();
        }
    }

    public class FSkeletalMeshAttributeVertexBuffer
    {
        public readonly int ComponentCount;
        public readonly EPixelFormat PixelFormat;
        public readonly int ComponentStride;
        public readonly FSkeletalMeshVertexAttributeData ValueData;

        public FSkeletalMeshAttributeVertexBuffer(FArchive Ar)
        {
            ComponentCount = Ar.Read<int>();
            PixelFormat = (EPixelFormat) Ar.Read<int>();
            ComponentStride = Ar.Read<int>();
            ValueData = new FSkeletalMeshVertexAttributeData(Ar);
        }
    }
}
