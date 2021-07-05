using System.Runtime.InteropServices;

namespace CUE4Parse.UE4.Objects.Core.Math
{
    [StructLayout(LayoutKind.Sequential)]
    public struct FSphere : IUStruct
    {
        /** The sphere's center point. */
        public FVector Center;
        /** The sphere's radius. */
        public float W;

        public FSphere(float x, float y, float z, float w)
        {
            Center = new FVector(x, y, z);
            W = w;
        }
    }
}