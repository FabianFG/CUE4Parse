using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Objects.Core.Math;
using CUE4Parse.UE4.Objects.UObject;

namespace CUE4Parse.UE4.Assets.Exports.Texture;

public class UPaperSprite : UObject
{
    public FVector2D BakedSourceUV;
    public FVector2D BakedSourceDimension;
    public FPackageIndex? BakedSourceTexture;
    public FPackageIndex? DefaultMaterial;
    public float PixelsPerUnrealUnit;
    public FVector4[] BakedRenderData;

    public override void Deserialize(FAssetArchive Ar, long validPos)
    {
        base.Deserialize(Ar, validPos);

        BakedSourceUV = GetOrDefault(nameof(BakedSourceUV), FVector2D.ZeroVector);
        BakedSourceDimension = GetOrDefault(nameof(BakedSourceDimension), FVector2D.ZeroVector);
        BakedSourceTexture = GetOrDefault<FPackageIndex>(nameof(BakedSourceTexture));
        DefaultMaterial = GetOrDefault<FPackageIndex>(nameof(DefaultMaterial));
        PixelsPerUnrealUnit = GetOrDefault(nameof(PixelsPerUnrealUnit), 1f);
        BakedRenderData = GetOrDefault<FVector4[]>(nameof(BakedRenderData), []);
    }
}
