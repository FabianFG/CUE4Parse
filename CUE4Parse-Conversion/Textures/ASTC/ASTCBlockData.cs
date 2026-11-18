using System.Runtime.CompilerServices;

namespace CUE4Parse_Conversion.Textures.ASTC;

public static partial class ASTCDecoder
{
    private struct BlockData
    {
        public int BlockWidth;
        public int BlockHeight;
        public int Width;
        public int Height;
        public int PartNum;
        public int DualPlane;
        public int PlaneSelector;
        public int WeightRange;
        /// <summary>
        /// max: 120
        /// </summary>
        public int WeightNum;
        public int WeightBits;
        public IntBuffer4 Cem;
        public int CemRange;
        /// <summary>
        /// max: 32
        /// </summary>
        public int EndpointValueNum;
        /// <summary>
        /// 4 * 8
        /// </summary>
        public EndpointBuffer Endpoints;
        public IntBuffer288 Weights;
        public IntBuffer144 Partition;
    }

    private struct IntSeqData
    {
        public int Bits;
        public int Nonbits;
    }

    [InlineArray(4)]
    private struct IntBuffer4
    {
        private int _element0;
    }

    [InlineArray(144)]
    private struct IntBuffer144
    {
        private int _element0;
    }

    [InlineArray(288)]
    private struct IntBuffer288
    {
        private int _element0;
    }

    [InlineArray(4)]
    private struct EndpointBuffer
    {
        private Endpoint _element0;

        [InlineArray(8)]
        public struct Endpoint
        {
            private int _element0;
        }
    }
}
