using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Objects.Core.Math;
using CUE4Parse.UE4.Objects.UObject;
using CUE4Parse.UE4.Versions;

namespace CUE4Parse.UE4.Objects.Engine;

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
        ItemName = Ar.ReadFName();
        Material = new FPackageIndex(Ar);
        iLink = Ar.Read<int>();
        iBrushPoly = Ar.Read<int>();
        LightMapScale = Ar.Read<float>();

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