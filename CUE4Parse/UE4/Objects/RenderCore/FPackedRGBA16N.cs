using CUE4Parse.UE4.Objects.Core.Math;
using CUE4Parse.UE4.Versions;
using CUE4Parse.UE4.Readers;
using Newtonsoft.Json;

namespace CUE4Parse.UE4.Objects.RenderCore
{
    [JsonConverter(typeof(FPackedRGBA16NConverter))]
    public class FPackedRGBA16N
    {
        public readonly uint X;
        public readonly uint Y;
        public readonly uint Z;
        public readonly uint W;

        public FPackedRGBA16N(FArchive Ar)
        {
            X = Ar.Read<ushort>();
            Y = Ar.Read<ushort>();
            Z = Ar.Read<ushort>();
            W = Ar.Read<ushort>();

            if (Ar.Game >= EGame.GAME_UE4_20)
            {
                X ^= 0x8000;
                Y ^= 0x8000;
                Z ^= 0x8000;
                W ^= 0x8000;
            }
        }

        public static explicit operator FVector(FPackedRGBA16N packedRGBA16N)
        {
            var X = (packedRGBA16N.X - (float) 32767.5) / (float) 32767.5;
            var Y = (packedRGBA16N.Y - (float) 32767.5) / (float) 32767.5;
            var Z = (packedRGBA16N.Z - (float) 32767.5) / (float) 32767.5;

            return new FVector(X, Y, Z);
        }

        public static explicit operator FVector4(FPackedRGBA16N packedRGBA16N)
        {
            var X = (packedRGBA16N.X - (float) 32767.5) / (float) 32767.5;
            var Y = (packedRGBA16N.Y - (float) 32767.5) / (float) 32767.5;
            var Z = (packedRGBA16N.Z - (float) 32767.5) / (float) 32767.5;
            var W = (packedRGBA16N.W - (float) 32767.5) / (float) 32767.5;

            return new FVector4(X, Y, Z, W);
        }

        public static explicit operator FPackedNormal(FPackedRGBA16N packedRGBA16N)
        {
            return new((FVector) packedRGBA16N);
        }
    }
}
