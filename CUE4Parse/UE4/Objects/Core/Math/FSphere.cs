using CUE4Parse.UE4.Readers;
using CUE4Parse.UE4.Versions;

namespace CUE4Parse.UE4.Objects.Core.Math
{
    public class FSphere : IUStruct
    {
        /** The sphere's center point. */
        public FVector Center;
        /** The sphere's radius. */
        public float W;

        public FSphere() { }
        public FSphere(float x, float y, float z, float w)
        {
            Center = new FVector(x, y, z);
            W = w;
        }
        public FSphere(FVector center, float w)
        {
            Center = center;
            W = w;
        }
        public FSphere(TIntVector3<float> center, float w)
        {
            Center = new FVector(center.X, center.Y, center.Z);
            W = w;
        }
        public FSphere(TIntVector3<double> center, double w)
        {
            Center = new FVector(center.X, center.Y, center.Z);
            W = (float) w;
        }
        public FSphere(FArchive Ar)
        {
            Center = new FVector(Ar);
            W = Ar.ReadFReal();
        }

        public static FSphere operator *(FSphere a, float scale) => new FSphere(a.Center * scale, a.W * scale);
    }
}
