using System.Runtime.CompilerServices;

namespace CUE4Parse_Conversion.Textures.ASTC
{
    public struct TexelWeightParams
    {
        public int Width;
        public int Height;
        public bool DualPlane;
        public int MaxWeight;
        public bool Error;
        public bool VoidExtentLDR;
        public bool VoidExtentHDR;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int GetPackedBitSize()
        {
            int Indices = Height * Width;
            if (DualPlane)
            {
                Indices *= 2;
            }

            IntegerEncoded IntEncoded = IntegerEncoded.CreateEncoding(MaxWeight);
            return IntEncoded.GetBitLength(Indices);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int GetNumWeightValues()
        {
            int Ret = Width * Height;
            if (DualPlane)
            {
                Ret *= 2;
            }
            return Ret;
        }
    }
}
