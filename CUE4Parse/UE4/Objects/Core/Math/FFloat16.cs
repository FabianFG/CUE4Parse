using System.Runtime.InteropServices;
using CUE4Parse.UE4.Readers;

namespace CUE4Parse.UE4.Objects.Core.Math
{
    [StructLayout(LayoutKind.Sequential)]
    public class FFloat16 : IUStruct
    {
        public ushort Encoded;

        public FFloat16()
        {
            Encoded = 0;
        }

        public FFloat16(FFloat16 FP16Value)
        {
            Encoded = FP16Value.Encoded;
        }

        public FFloat16(FArchive Ar)
        {
            Encoded = Ar.Read<ushort>();
        }
    }
}
