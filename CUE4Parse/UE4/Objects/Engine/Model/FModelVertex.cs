using CUE4Parse.UE4.Objects.Core.Math;
using CUE4Parse.UE4.Objects.RenderCore;
using CUE4Parse.UE4.Versions;
using CUE4Parse.UE4.Readers;

namespace CUE4Parse.UE4.Objects.Engine.Model
{
    public readonly struct FModelVertex : IUStruct
    {
        public readonly FVector Position;
        public readonly FVector TangentX;
        public readonly FVector4 TangentZ;
        public readonly FVector2D TexCoord;
        public readonly FVector2D ShadowTexCoord;

        public FModelVertex(FArchive Ar)
        {
            if(Ar.Game <= EGame.GAME_UE4_19) // before ue4.20
            {
                Position = Ar.Read<FVector>();
                TangentX = (FVector) new FPackedNormal(Ar);
                TangentZ = (FVector4) new FPackedNormal(Ar);
                TexCoord = Ar.Read<FVector2D>();
                ShadowTexCoord = Ar.Read<FVector2D>();
            }
            else
            {
                Position = Ar.Read<FVector>();
                TangentX = Ar.Read<FVector>();
                TangentZ = Ar.Read<FVector4>();
                TexCoord = Ar.Read<FVector2D>();
                ShadowTexCoord = Ar.Read<FVector2D>();
            }
        }

        FVector GetTangentY()
        {
            return ((FVector) TangentZ ^ TangentX) * TangentZ.W;
        }
    }
}