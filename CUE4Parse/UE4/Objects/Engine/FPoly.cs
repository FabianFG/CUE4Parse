using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Objects.Core.Math;
using CUE4Parse.UE4.Objects.UObject;

namespace CUE4Parse.UE4.Objects.Engine;

public class FPoly
{
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
        Base = Ar.Read<FVector>();
        Normal = Ar.Read<FVector>();
        TextureU = Ar.Read<FVector>();
        TextureV = Ar.Read<FVector>();
        Vertices = Ar.ReadArray<FVector>();
        PolyFlags = Ar.Read<uint>();
        Actor = new FPackageIndex(Ar);
        ItemName = Ar.ReadFName();
        Material = new FPackageIndex(Ar);
        iLink = Ar.Read<int>();
        iBrushPoly = Ar.Read<int>();
        LightMapScale = Ar.Read<float>();
        LightmassSettings = new FLightmassPrimitiveSettings(Ar);
        RulesetVariation = Ar.ReadFName();
    }
}