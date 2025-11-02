using System;
using CUE4Parse.UE4.Assets.Exports.BuildData;
using CUE4Parse.UE4.Assets.Exports.Texture;
using CUE4Parse.UE4.Assets.Objects;
using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Objects.Core.Math;
using CUE4Parse.UE4.Objects.Core.Misc;
using CUE4Parse.UE4.Versions;

namespace CUE4Parse.UE4.Assets.Exports.Component.Landscape;

public class ULandscapeComponent: UPrimitiveComponent
{
    public int SectionBaseX;
    public int SectionBaseY;
    public int ComponentSizeQuads;
    public int SubsectionSizeQuads;
    public int NumSubsections;
    public FVector4 HeightmapScaleBias;
    public FVector4 WeightmapScaleBias;
    public float WeightmapSubsectionOffset;
    public FWeightmapLayerAllocationInfo[] WeightmapLayerAllocations;
    public FBox CachedLocalBox;
    public FGuid MapBuildDataId;

    public Lazy<UTexture2D[]> WeightmapTextures;

    public FMeshMapBuildData? LegacyMapBuildData;
    public FLandscapeComponentGrassData GrassData;
    public bool bCooked;
    public FLandscapeComponentDerivedData? PlatformData;

    public override void Deserialize(FAssetArchive Ar, long validPos)
    {
        if (Ar.Game == EGame.GAME_WorldofJadeDynasty) Ar.Position += 20;
        base.Deserialize(Ar, validPos);
        SectionBaseX = GetOrDefault(nameof(SectionBaseX), 0);
        SectionBaseY = GetOrDefault(nameof(SectionBaseY), 0);
        ComponentSizeQuads = GetOrDefault(nameof(ComponentSizeQuads), 0);
        SubsectionSizeQuads = GetOrDefault(nameof(SubsectionSizeQuads), 0);
        NumSubsections = GetOrDefault(nameof(NumSubsections), 1);
        HeightmapScaleBias = GetOrDefault(nameof(HeightmapScaleBias), new FVector4(0, 0, 0, 0));
        WeightmapScaleBias = GetOrDefault(nameof(WeightmapScaleBias), new FVector4(0, 0, 0, 0));
        WeightmapSubsectionOffset = GetOrDefault(nameof(WeightmapSubsectionOffset), 0f);
        WeightmapLayerAllocations = GetOrDefault(nameof(WeightmapLayerAllocations), Array.Empty<FWeightmapLayerAllocationInfo>());
        CachedLocalBox = GetOrDefault<FBox>(nameof(CachedLocalBox));
        MapBuildDataId = GetOrDefault<FGuid>(nameof(MapBuildDataId));
        WeightmapTextures = new Lazy<UTexture2D[]>(() => GetOrDefault<UTexture2D[]>("WeightmapTextures", []));

        if (FRenderingObjectVersion.Get(Ar) < FRenderingObjectVersion.Type.MapBuildDataSeparatePackage)
        {
            LegacyMapBuildData = new FMeshMapBuildData();
            LegacyMapBuildData.LightMap = new FLightMap(Ar);
            LegacyMapBuildData.ShadowMap = new FShadowMap(Ar);
        }

        if (Ar.Ver >= EUnrealEngineObjectUE4Version.SERIALIZE_LANDSCAPE_GRASS_DATA)
        {
            GrassData = new FLandscapeComponentGrassData(Ar);
        }

        if (Ar.IsFilterEditorOnly)
        {
            Ar.Position += sizeof(int); // SelectedType
        }

        if (Ar.Game is EGame.GAME_Farlight84) Ar.Position += 32;
        
        if (Ar.Ver >= EUnrealEngineObjectUE4Version.LANDSCAPE_PLATFORMDATA_COOKING && !Flags.HasFlag(EObjectFlags.RF_ClassDefaultObject))
        {
            bCooked = Ar.ReadBoolean();
        }

        if (Ar.Game < EGame.GAME_UE5_1 && Ar.Position + 4 <= validPos)
        {
            var bCookedMobileData = Ar.ReadBoolean();
            if (bCookedMobileData)
            {
                PlatformData = new FLandscapeComponentDerivedData(Ar);
            }
        }
    }

    public void GetComponentExtent(ref int minX, ref int minY, ref int maxX, ref int maxY)
    {
        minX = Math.Min(SectionBaseX, minX);
        minY = Math.Min(SectionBaseY, minY);
        maxX = Math.Max(SectionBaseX + ComponentSizeQuads, maxX);
        maxY = Math.Max(SectionBaseY + ComponentSizeQuads, maxY);
    }

    public FIntRect GetComponentExtent()
    {
        int minX = int.MaxValue, minY = int.MaxValue;
        int maxX = int.MinValue, maxY = int.MinValue;
        GetComponentExtent(ref minX, ref minY, ref maxX, ref maxY);
        return new FIntRect(new FIntPoint(minX, minY), new FIntPoint(maxX, maxY));
    }

    public UTexture2D? GetHeightmap() => GetOrDefault<UTexture2D>("HeightmapTexture", null);
    public UTexture2D[] GetWeightmapTextures() => WeightmapTextures.Value;

    public FWeightmapLayerAllocationInfo[] GetWeightmapLayerAllocations() => WeightmapLayerAllocations;
}

public class FLandscapeComponentDerivedData
{
    public byte[] CompressedLandscapeData;
    public FByteBulkData[]? StreamingLODDataArray;

    public FLandscapeComponentDerivedData(FAssetArchive Ar)
    {
        CompressedLandscapeData = Ar.ReadArray<byte>();
        if (Ar.Game >= EGame.GAME_UE4_26)
        {
            StreamingLODDataArray = Ar.ReadArray(() => new FByteBulkData(Ar));
        }
    }
}
