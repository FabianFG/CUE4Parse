namespace CUE4Parse.UE4.Objects.Engine
{
    public readonly struct FLightmassPrimitiveSettings : IUStruct
    {
        public readonly bool bUseTwoSidedLighting;
        public readonly bool bShadowIndirectOnly;
        public readonly bool bUseEmissiveForStaticLighting;
        public readonly bool bUseVertexNormalForHemisphereGather;
        public readonly float EmissiveLightFalloffExponent;
        public readonly float EmissiveLightExplicitInfluenceRadius;
        public readonly float EmissiveBoost;
        public readonly float DiffuseBoost;
        public readonly float FullyOccludedSamplesFraction;
    }
}