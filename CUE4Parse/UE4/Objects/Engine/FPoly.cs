using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Objects.Core.Math;
using CUE4Parse.UE4.Objects.UObject;
using CUE4Parse.UE4.Versions;

namespace CUE4Parse.UE4.Objects.Engine;

public struct FLightingChannelContainer
{
    public byte Initialized;
    public byte BSP;
    public byte Static;
    public byte Dynamic;
}

public class FPoly
{
    public int VertexCount;
    public FVector Base;
    public FVector Normal;
    public FVector TextureU;
    public FVector TextureV;
    public FVector[] Vertices;
    public uint PolyFlags;
    public FPackageIndex Actor;
    public FName ItemName;
    public FPackageIndex Material;
    public int iLink;
    public int iBrushPoly;
    public float LightMapScale;
    public FLightmassPrimitiveSettings LightmassSettings;
    public FName RulesetVariation;
    
    public FPoly(FAssetArchive Ar)
    {
        if (Ar.Ver < EUnrealEngineObjectUE3Version.FPOLYVERTEXARRAY)
        {
            VertexCount = Ar.Read<int>();
        }
        Base = Ar.Read<FVector>();
        Normal = Ar.Read<FVector>();
        TextureU = Ar.Read<FVector>();
        TextureV = Ar.Read<FVector>();
        if (Ar.Ver < EUnrealEngineObjectUE3Version.FPOLYVERTEXARRAY)
        {
            Vertices = Ar.ReadArray<FVector>(VertexCount);
        }
        else
        {
            Vertices = Ar.ReadArray<FVector>();
        }
        PolyFlags = Ar.Read<uint>();
        Actor = new FPackageIndex(Ar);
        if (Ar.Ver < EUnrealEngineObjectUE3Version.TextureDeprecatedFromPoly)
        {
            Material = new FPackageIndex(Ar);
        }
        ItemName = Ar.ReadFName();
        if (Ar.Ver >= EUnrealEngineObjectUE3Version.TextureDeprecatedFromPoly)
        {
            Material = new FPackageIndex(Ar);
        }
        iLink = Ar.Read<int>();
        iBrushPoly = Ar.Read<int>();

        if (Ar.Ver < EUnrealEngineObjectUE3Version.PanUVRemovedFromPoly)
        {
            Ar.Read<short>(); // PanU
            Ar.Read<short>(); // PanV
        }

        if (Ar.Ver >= EUnrealEngineObjectUE3Version.LightMapScaleAddedToPoly && Ar.Ver < EUnrealEngineObjectUE3Version.TWOSIDEDSIGN_PARAMETERS)
        {
            LightMapScale = Ar.Read<float>();
        }

        if (Ar.Ver >= EUnrealEngineObjectUE3Version.TWOSIDEDSIGN_PARAMETERS && Ar.Game < GAME_UE4_0)
        {
            Ar.Read<float>(); // ShadowMapScale
        }

        if (Ar.Ver >= EUnrealEngineObjectUE3Version.BSP_LIGHTING_CHANNEL_SUPPORT && Ar.Game < GAME_UE4_0)
        {
            Ar.Read<FLightingChannelContainer>(); // LightingChannels
        }

        if (Ar.Ver >= EUnrealEngineObjectUE3Version.INTEGRATED_LIGHTMASS)
        {
            LightmassSettings = new FLightmassPrimitiveSettings(Ar);
        }

        if (Ar.Ver >= EUnrealEngineObjectUE3Version.ADD_FPOLY_PBRULESET_POINTER && Ar.Ver < EUnrealEngineObjectUE3Version.FPOLY_RULESET_VARIATIONNAME)
        {
            new FPackageIndex(Ar); // Ruleset
        }
        else
        {
            RulesetVariation = Ar.ReadFName();
        }
    }
}