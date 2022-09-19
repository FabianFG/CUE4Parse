using CUE4Parse.UE4.Readers;
using CUE4Parse.UE4.Versions;

namespace CUE4Parse.UE4.Objects.Core.Math
{
    public class FSphere
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

        public FSphere(FVector center, float w)
        {
            Center = center;
            W = w;
        }

        public FSphere(FArchive Ar)
        {
            Center = new FVector(Ar);
            if (Ar.Ver >= EUnrealEngineObjectUE5Version.LARGE_WORLD_COORDINATES)
            {
                W = (float) Ar.Read<double>();
            }
            else
            {
                W = Ar.Read<float>();
            }
        }

        public static FSphere operator *(FSphere a, float scale) => new FSphere(a.Center * scale, a.W * scale);
    }
}
