using CUE4Parse.UE4.Objects.Core.Math;

namespace CUE4Parse.UE4.Objects.Engine.Model
{
    public readonly struct FModelVertex : IUStruct
    {
        public readonly FVector Position;
        public readonly FVector TangentX;
        public readonly FVector4 TangentZ;
        public readonly FVector2D TexCoord;
        public readonly FVector2D ShadowTexCoord;

        FVector GetTangentY()
        {
            return ((FVector) TangentZ ^ TangentX) * TangentZ.W;
        }
    }
}