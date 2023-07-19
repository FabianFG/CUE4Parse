using CUE4Parse.UE4.Readers;
using CUE4Parse.UE4.Versions;

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

        public FLightmassPrimitiveSettings(FArchive Ar)
        {
            bUseTwoSidedLighting = Ar.ReadBoolean();
            bShadowIndirectOnly = Ar.ReadBoolean();
            FullyOccludedSamplesFraction = Ar.Read<float>();
            bUseEmissiveForStaticLighting = Ar.ReadBoolean();
            bUseVertexNormalForHemisphereGather = Ar.Ver >= EUnrealEngineObjectUE4Version.NEW_LIGHTMASS_PRIMITIVE_SETTING && Ar.ReadBoolean();
            EmissiveLightFalloffExponent = Ar.Read<float>();
            EmissiveLightExplicitInfluenceRadius = Ar.Read<float>();
            EmissiveBoost = Ar.Read<float>();
            DiffuseBoost = Ar.Read<float>();
        }
    }
}