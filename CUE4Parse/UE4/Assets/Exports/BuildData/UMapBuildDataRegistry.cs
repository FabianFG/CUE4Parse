using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Exceptions;
using CUE4Parse.UE4.Objects.Core.Math;
using CUE4Parse.UE4.Objects.Core.Misc;
using CUE4Parse.UE4.Objects.Engine;
using CUE4Parse.UE4.Objects.UObject;
using CUE4Parse.UE4.Readers;
using CUE4Parse.UE4.Versions;
using Newtonsoft.Json;

namespace CUE4Parse.UE4.Assets.Exports.BuildData
{
    public class UMapBuildDataRegistry : UObject
    {
        public Dictionary<FGuid, FMeshMapBuildData>? MeshBuildData;
        public Dictionary<FGuid, FPrecomputedLightVolumeData>? LevelPrecomputedLightVolumeBuildData;
        public Dictionary<FGuid, FPrecomputedVolumetricLightmapData>? LevelPrecomputedVolumetricLightmapBuildData;
        public Dictionary<FGuid, FLightComponentMapBuildData>? LightBuildData;
        public Dictionary<FGuid, FReflectionCaptureMapBuildData>? ReflectionCaptureBuildData;
        public Dictionary<FGuid, FSkyAtmosphereMapBuildData>? SkyAtmosphereBuildData;

        public override void Deserialize(FAssetArchive Ar, long validPos)
        {
            base.Deserialize(Ar, validPos);

            var stripFlags = new FStripDataFlags(Ar);

            if (!stripFlags.IsDataStrippedForServer())
            {
                var numEntries = Ar.Read<int>();
                MeshBuildData = new Dictionary<FGuid, FMeshMapBuildData>(numEntries);
                for (var i = 0; i < numEntries; ++i)
                {
                    MeshBuildData[Ar.Read<FGuid>()] = new FMeshMapBuildData(Ar);
                }

                numEntries = Ar.Read<int>();
                LevelPrecomputedLightVolumeBuildData = new Dictionary<FGuid, FPrecomputedLightVolumeData>(numEntries);
                for (var i = 0; i < numEntries; ++i)
                {
                    LevelPrecomputedLightVolumeBuildData[Ar.Read<FGuid>()] = new FPrecomputedLightVolumeData(Ar);
                }

                if (FRenderingObjectVersion.Get(Ar) >= FRenderingObjectVersion.Type.VolumetricLightmaps)
                {
                    numEntries = Ar.Read<int>();
                    LevelPrecomputedVolumetricLightmapBuildData = new Dictionary<FGuid, FPrecomputedVolumetricLightmapData>(numEntries);
                    for (var i = 0; i < numEntries; ++i)
                    {
                        LevelPrecomputedVolumetricLightmapBuildData[Ar.Read<FGuid>()] = new FPrecomputedVolumetricLightmapData(Ar);
                    }
                }

                numEntries = Ar.Read<int>();
                LightBuildData = new Dictionary<FGuid, FLightComponentMapBuildData>(numEntries);
                for (var i = 0; i < numEntries; ++i)
                {
                    LightBuildData[Ar.Read<FGuid>()] = new FLightComponentMapBuildData(Ar);
                }

                if (FReflectionCaptureObjectVersion.Get(Ar) >= FReflectionCaptureObjectVersion.Type.MoveReflectionCaptureDataToMapBuildData)
                {
                    numEntries = Ar.Read<int>();
                    ReflectionCaptureBuildData = new Dictionary<FGuid, FReflectionCaptureMapBuildData>(numEntries);
                    for (var i = 0; i < numEntries; ++i)
                    {
                        ReflectionCaptureBuildData[Ar.Read<FGuid>()] = new FReflectionCaptureMapBuildData(Ar);
                    }
                }

                if (FRenderingObjectVersion.Get(Ar) >= FRenderingObjectVersion.Type.SkyAtmosphereStaticLightingVersioning)
                {
                    numEntries = Ar.Read<int>();
                    SkyAtmosphereBuildData = new Dictionary<FGuid, FSkyAtmosphereMapBuildData>(numEntries);
                    for (var i = 0; i < numEntries; ++i)
                    {
                        SkyAtmosphereBuildData[Ar.Read<FGuid>()] = new FSkyAtmosphereMapBuildData(Ar);
                    }
                }
            }
        }

        protected internal override void WriteJson(JsonWriter writer, JsonSerializer serializer)
        {
            base.WriteJson(writer, serializer);

            if (MeshBuildData?.Count > 0)
            {
                writer.WritePropertyName("MeshBuildData");
                serializer.Serialize(writer, MeshBuildData);
            }

            if (LevelPrecomputedLightVolumeBuildData?.Count > 0)
            {
                writer.WritePropertyName("LevelPrecomputedLightVolumeBuildData");
                serializer.Serialize(writer, LevelPrecomputedLightVolumeBuildData);
            }

            if (LevelPrecomputedVolumetricLightmapBuildData?.Count > 0)
            {
                writer.WritePropertyName("LevelPrecomputedVolumetricLightmapBuildData");
                serializer.Serialize(writer, LevelPrecomputedVolumetricLightmapBuildData);
            }

            if (LightBuildData?.Count > 0)
            {
                writer.WritePropertyName("LightBuildData");
                serializer.Serialize(writer, LightBuildData);
            }

            if (ReflectionCaptureBuildData?.Count > 0)
            {
                writer.WritePropertyName("ReflectionCaptureBuildData");
                serializer.Serialize(writer, ReflectionCaptureBuildData);
            }

            if (SkyAtmosphereBuildData?.Count > 0)
            {
                writer.WritePropertyName("SkyAtmosphereBuildData");
                serializer.Serialize(writer, SkyAtmosphereBuildData);
            }
        }
    }

    public class FSkyAtmosphereMapBuildData
    {
        // public bool bDummy;

        public FSkyAtmosphereMapBuildData(FArchive Ar)
        {
            // bDummy = Ar.ReadBoolean(); // Not serialized
        }
    }

    public class FReflectionCaptureMapBuildData : FReflectionCaptureData
    {
        public FReflectionCaptureMapBuildData(FAssetArchive Ar) : base(Ar) { }
    }

    [JsonConverter(typeof(FReflectionCaptureDataConverter))]
    public class FReflectionCaptureData
    {
        public int CubemapSize;
        public float AverageBrightness;
        public float Brightness;
        public byte[]? FullHDRCapturedData;
        public FPackageIndex? EncodedCaptureData;

        public FReflectionCaptureData(FAssetArchive Ar)
        {
            CubemapSize = Ar.Read<int>();
            AverageBrightness = Ar.Read<float>();

            if (FRenderingObjectVersion.Get(Ar) >= FRenderingObjectVersion.Type.StoreReflectionCaptureBrightnessForCooking &&
                FUE5ReleaseStreamObjectVersion.Get(Ar) < FUE5ReleaseStreamObjectVersion.Type.ExcludeBrightnessFromEncodedHDRCubemap)
            {
                Brightness = Ar.Read<float>();
            }

            FullHDRCapturedData = Ar.ReadArray<byte>(); // Can also be stripped, but still a byte[]

            if (FMobileObjectVersion.Get(Ar) >= FMobileObjectVersion.Type.StoreReflectionCaptureCompressedMobile &&
                FUE5ReleaseStreamObjectVersion.Get(Ar) < FUE5ReleaseStreamObjectVersion.Type.StoreReflectionCaptureEncodedHDRDataInRG11B10Format)
            {
                EncodedCaptureData = new FPackageIndex(Ar);
            }
            else
            {
                var _ = Ar.ReadArray<byte>();
            }
        }
    }

    public class FReflectionCaptureDataConverter : JsonConverter<FReflectionCaptureData>
    {
        public override void WriteJson(JsonWriter writer, FReflectionCaptureData value, JsonSerializer serializer)
        {
            writer.WriteStartObject();

            writer.WritePropertyName("CubemapSize");
            writer.WriteValue(value.CubemapSize);

            writer.WritePropertyName("AverageBrightness");
            writer.WriteValue(value.AverageBrightness);

            writer.WritePropertyName("Brightness");
            writer.WriteValue(value.Brightness);

            if (value.EncodedCaptureData != null)
            {
                writer.WritePropertyName("EncodedCaptureData");
                serializer.Serialize(writer, value.EncodedCaptureData);
            }

            writer.WriteEndObject();
        }

        public override FReflectionCaptureData ReadJson(JsonReader reader, Type objectType, FReflectionCaptureData existingValue, bool hasExistingValue,
            JsonSerializer serializer)
        {
            throw new NotImplementedException();
        }
    }

    public class FLightComponentMapBuildData
    {
        public int ShadowMapChannel;
        public FStaticShadowDepthMapData DepthMap;

        public FLightComponentMapBuildData(FArchive Ar)
        {
            ShadowMapChannel = Ar.Read<int>();
            DepthMap = new FStaticShadowDepthMapData(Ar);
        }
    }

    public class FStaticShadowDepthMapData
    {
        public FMatrix WorldToLight;
        public int ShadowMapSizeX;
        public int ShadowMapSizeY;
        public FFloat16[]? DepthSamples;

        public FStaticShadowDepthMapData(FArchive Ar)
        {
            WorldToLight = new FMatrix(Ar);
            ShadowMapSizeX = Ar.Read<int>();
            ShadowMapSizeY = Ar.Read<int>();
            DepthSamples = Ar.ReadArray(() => new FFloat16(Ar));
        }
    }

    public class FVolumeLightingSample
    {
        public FVector Position;
        public float Radius;
        public float[][] Lighting;
        public FColor PackedSkyBentNormal;
        public float DirectionalLightShadowing;

        public FVolumeLightingSample(FAssetArchive Ar)
        {
            Position = Ar.Read<FVector>();
            Radius = Ar.Read<float>();
            Lighting = Ar.ReadArray(3, () => Ar.ReadArray<float>(9));
            PackedSkyBentNormal = Ar.Read<FColor>();
            DirectionalLightShadowing = Ar.Read<float>();
        }
    }

    public class FPrecomputedLightVolumeData
    {
        public FBox Bounds;
        public float SampleSpacing;
        public int NumSHSamples;
        public FVolumeLightingSample[] HighQualitySamples;
        public FVolumeLightingSample[]? LowQualitySamples;

        public FPrecomputedLightVolumeData(FAssetArchive Ar)
        {
            var bValid = Ar.ReadBoolean();

            if (bValid)
            {
                var bVolumeInitialized = Ar.ReadBoolean();
                if (bVolumeInitialized)
                {
                    Bounds = new FBox(Ar);
                    SampleSpacing = Ar.Read<float>();
                    NumSHSamples = 4;
                    if (FRenderingObjectVersion.Get(Ar) >= FRenderingObjectVersion.Type.IndirectLightingCache3BandSupport)
                    {
                        NumSHSamples = Ar.Read<int>();
                    }

                    HighQualitySamples = Ar.ReadArray(() => new FVolumeLightingSample(Ar));
                    if (Ar.Ver >= EUnrealEngineObjectUE4Version.VOLUME_SAMPLE_LOW_QUALITY_SUPPORT)
                    {
                        LowQualitySamples = Ar.ReadArray(() => new FVolumeLightingSample(Ar));
                    }
                }
            }
        }
    }

    public class FPrecomputedVolumetricLightmapData
    {
        public FBox Bounds;
        public FIntVector IndirectionTextureDimensions;
        public FVolumetricLightmapDataLayer IndirectionTexture;
        public int BrickSize;
        public FIntVector BrickDataDimensions;
        public FVolumetricLightmapBrickLayer BrickData;
        public FIntVector[]? SubLevelBrickPositions;
        public FColor[]? IndirectionTextureOriginalValues;

        public FPrecomputedVolumetricLightmapData(FArchive Ar)
        {
            var bValid = Ar.ReadBoolean();

            if (bValid)
            {
                if (Ar.Game == EGame.GAME_StarWarsJediSurvivor) Ar.Position += 8;

                Bounds = new FBox(Ar);
                IndirectionTextureDimensions = Ar.Read<FIntVector>();
                IndirectionTexture = new FVolumetricLightmapDataLayer(Ar);

                BrickSize = Ar.Read<int>();
                BrickDataDimensions = Ar.Read<FIntVector>();

                BrickData = new FVolumetricLightmapBrickLayer
                {
                    AmbientVector = new FVolumetricLightmapDataLayer(Ar),
                    SHCoefficients = new FVolumetricLightmapDataLayer[6]
                };

                for (var i = 0; i < BrickData.SHCoefficients.Length; i++)
                {
                    BrickData.SHCoefficients[i] = new FVolumetricLightmapDataLayer(Ar);
                }

                BrickData.SkyBentNormal = new FVolumetricLightmapDataLayer(Ar);
                BrickData.DirectionalLightShadowing = new FVolumetricLightmapDataLayer(Ar);

                if (FMobileObjectVersion.Get(Ar) >= FMobileObjectVersion.Type.LQVolumetricLightmapLayers)
                {
                    BrickData.LQLightColor = new FVolumetricLightmapDataLayer(Ar);
                    BrickData.LQLightDirection = new FVolumetricLightmapDataLayer(Ar);
                }

                if (FRenderingObjectVersion.Get(Ar) >= FRenderingObjectVersion.Type.VolumetricLightmapStreaming)
                {
                    SubLevelBrickPositions = Ar.ReadArray<FIntVector>();
                    IndirectionTextureOriginalValues = Ar.ReadArray<FColor>();
                }
            }
        }
    }

    public class FVolumetricLightmapBasicBrickDataLayers
    {
        public FVolumetricLightmapDataLayer? AmbientVector;
        public FVolumetricLightmapDataLayer[]? SHCoefficients;
        public FVolumetricLightmapDataLayer? SkyBentNormal;
        public FVolumetricLightmapDataLayer? DirectionalLightShadowing;
    }

    public class FVolumetricLightmapBrickLayer : FVolumetricLightmapBasicBrickDataLayers
    {
        // Mobile LQ Layers:
        public FVolumetricLightmapDataLayer? LQLightColor;
        public FVolumetricLightmapDataLayer? LQLightDirection;
    }

    public class FVolumetricLightmapDataLayer
    {
        public byte[] Data;
        public string PixelFormatString;

        public FVolumetricLightmapDataLayer(FArchive Ar)
        {
            Data = Ar.ReadArray<byte>();
            PixelFormatString = Ar.ReadFString();
        }
    }

    [JsonConverter(typeof(FMeshMapBuildDataConverter))]
    public class FMeshMapBuildData
    {
        public FLightMap? LightMap;
        public FShadowMap? ShadowMap;
        public FGuid[] IrrelevantLights;
        public FPerInstanceLightmapData[] PerInstanceLightmapData;

        public FMeshMapBuildData(FAssetArchive Ar)
        {
            var LightMapType = Ar.Read<ELightMapType>();
            switch (LightMapType)
            {
                case ELightMapType.LMT_None:
                    LightMap = null;
                    break;
                case ELightMapType.LMT_1D:
                    LightMap = new FLegacyLightMap1D(Ar);
                    break;
                case ELightMapType.LMT_2D:
                    LightMap = new FLightMap2D(Ar);
                    break;
            }

            var ShadowMapType = Ar.Read<EShadowMapType>();
            switch (ShadowMapType)
            {
                case EShadowMapType.SMT_None:
                    ShadowMap = null;
                    break;
                case EShadowMapType.SMT_2D:
                    ShadowMap = new FShadowMap2D(Ar);
                    break;
            }

            IrrelevantLights = Ar.ReadArray<FGuid>();
            PerInstanceLightmapData = Ar.ReadBulkArray<FPerInstanceLightmapData>();
        }
    }

    public class FMeshMapBuildDataConverter : JsonConverter<FMeshMapBuildData>
    {
        public override void WriteJson(JsonWriter writer, FMeshMapBuildData value, JsonSerializer serializer)
        {
            writer.WriteStartObject();

            if (value.LightMap != null)
            {
                writer.WritePropertyName("LightMap");
                serializer.Serialize(writer, value.LightMap);
            }

            if (value.ShadowMap != null)
            {
                writer.WritePropertyName("ShadowMap");
                serializer.Serialize(writer, value.ShadowMap);
            }

            writer.WritePropertyName("IrrelevantLights");
            serializer.Serialize(writer, value.IrrelevantLights);

            writer.WritePropertyName("PerInstanceLightmapData");
            serializer.Serialize(writer, value.PerInstanceLightmapData);

            writer.WriteEndObject();
        }

        public override FMeshMapBuildData ReadJson(JsonReader reader, Type objectType, FMeshMapBuildData existingValue, bool hasExistingValue,
            JsonSerializer serializer)
        {
            throw new NotImplementedException();
        }
    }

    public enum ELightMapType : uint
    {
        LMT_None = 0,
        LMT_1D = 1,
        LMT_2D = 2,
    };
    public class FLightMap
    {
        public readonly FGuid[] LightGuids;

        public FLightMap(FAssetArchive Ar)
        {
            LightGuids = Ar.ReadArray<FGuid>();
        }
    }
    public class FLegacyLightMap1D : FLightMap
    {
        public FLegacyLightMap1D(FAssetArchive Ar) : base(Ar)
        {
            throw new ParserException("Unsupported: FLegacyLightMap1D");
        }
    }

    [JsonConverter(typeof(FLightMap2DConverter))]
    public class FLightMap2D : FLightMap
    {
        const int NUM_STORED_LIGHTMAP_COEF = 4;
        public readonly FPackageIndex[]? Textures;
        public readonly FPackageIndex? SkyOcclusionTexture;
        public readonly FPackageIndex? AOMaterialMaskTexture;
        public readonly FPackageIndex? ShadowMapTexture;
        public readonly FPackageIndex[]? VirtualTextures;
        public readonly FVector4[]? ScaleVectors;
        public readonly FVector4[]? AddVectors;
        public readonly FVector2D? CoordinateScale;
        public readonly FVector2D? CoordinateBias;
        public readonly FVector4? InvUniformPenumbraSize;
        public readonly bool[]? bShadowChannelValid;

        public FLightMap2D(FAssetArchive Ar): base(Ar)
        {
            Textures = new FPackageIndex[2];
            VirtualTextures = new FPackageIndex[2];
            ScaleVectors = new FVector4[NUM_STORED_LIGHTMAP_COEF];
            AddVectors = new FVector4[NUM_STORED_LIGHTMAP_COEF];
            if (Ar.Ver <= EUnrealEngineObjectUE4Version.LOW_QUALITY_DIRECTIONAL_LIGHTMAPS)
            {
                for (var CoefficientIndex = 0; CoefficientIndex < 3; CoefficientIndex++)
                {
                    Ar.Position += 36;
                }
            }
            else if (Ar.Ver <= EUnrealEngineObjectUE4Version.COMBINED_LIGHTMAP_TEXTURES)
            {
                for (var CoefficientIndex = 0; CoefficientIndex < 4; CoefficientIndex++)
                {
                    Ar.Position += 36;
                }
            }
            else
            {
                Textures[0] = new FPackageIndex(Ar);
                Textures[1] = new FPackageIndex(Ar);

                if (Ar.Ver >= EUnrealEngineObjectUE4Version.SKY_LIGHT_COMPONENT)
                {
                    SkyOcclusionTexture = new FPackageIndex(Ar);
                    if (Ar.Ver >= EUnrealEngineObjectUE4Version.AO_MATERIAL_MASK)
                    {
                        AOMaterialMaskTexture = new FPackageIndex(Ar);
                    }
                }

                for (var CoefficientIndex = 0; CoefficientIndex < NUM_STORED_LIGHTMAP_COEF; CoefficientIndex++)
                {
                    ScaleVectors[CoefficientIndex] = Ar.Read<FVector4>();
                    AddVectors[CoefficientIndex] = Ar.Read<FVector4>();
                }
            }
            CoordinateScale = new FVector2D(Ar);
            CoordinateBias = new FVector2D(Ar);

            if (FRenderingObjectVersion.Get(Ar) >= FRenderingObjectVersion.Type.LightmapHasShadowmapData)
            {
                bShadowChannelValid = Ar.ReadArray(4, () => Ar.ReadBoolean());
                InvUniformPenumbraSize = Ar.Read<FVector4>();
            }

            if (FRenderingObjectVersion.Get(Ar) >= FRenderingObjectVersion.Type.VirtualTexturedLightmaps)
            {
                if (FRenderingObjectVersion.Get(Ar) >= FRenderingObjectVersion.Type.VirtualTexturedLightmapsV2)
                {
                    if (FRenderingObjectVersion.Get(Ar) >= FRenderingObjectVersion.Type.VirtualTexturedLightmapsV3)
                    {
                        VirtualTextures[0] = new FPackageIndex(Ar);
                        VirtualTextures[1] = new FPackageIndex(Ar);
                    }
                    else
                    {
                        VirtualTextures[0] = new FPackageIndex(Ar);
                    }
                }
                else
                {
                    VirtualTextures[0] = new FPackageIndex(Ar);
                }
            }
        }
    }

    public class FLightMap2DConverter : JsonConverter<FLightMap2D>
    {
        public override void WriteJson(JsonWriter writer, FLightMap2D value, JsonSerializer serializer)
        {
            writer.WriteStartObject();

            writer.WritePropertyName("Textures");
            serializer.Serialize(writer, value.Textures);

            if (!value.SkyOcclusionTexture?.IsNull ?? false)
            {
                writer.WritePropertyName("SkyOcclusionTexture");
                serializer.Serialize(writer, value.SkyOcclusionTexture);
            }

            if (!value.AOMaterialMaskTexture?.IsNull ?? false)
            {
                writer.WritePropertyName("AOMaterialMaskTexture");
                serializer.Serialize(writer, value.AOMaterialMaskTexture);
            }

            if (!value.ShadowMapTexture?.IsNull ?? false)
            {
                writer.WritePropertyName("ShadowMapTexture");
                serializer.Serialize(writer, value.ShadowMapTexture);
            }

            writer.WritePropertyName("VirtualTextures");
            serializer.Serialize(writer, value.VirtualTextures);

            writer.WritePropertyName("ScaleVectors");
            serializer.Serialize(writer, value.ScaleVectors);

            writer.WritePropertyName("AddVectors");
            serializer.Serialize(writer, value.AddVectors);

            writer.WritePropertyName("CoordinateScale");
            serializer.Serialize(writer, value.CoordinateScale);

            writer.WritePropertyName("CoordinateBias");
            serializer.Serialize(writer, value.CoordinateBias);

            writer.WritePropertyName("InvUniformPenumbraSize");
            serializer.Serialize(writer, value.InvUniformPenumbraSize);

            writer.WritePropertyName("bShadowChannelValid");
            serializer.Serialize(writer, value.bShadowChannelValid);

            /*
             * FLightMap
             */
            writer.WritePropertyName("LightGuids");
            serializer.Serialize(writer, value.LightGuids);

            writer.WriteEndObject();
        }

        public override FLightMap2D ReadJson(JsonReader reader, Type objectType, FLightMap2D existingValue, bool hasExistingValue,
            JsonSerializer serializer)
        {
            throw new NotImplementedException();
        }
    }

    public enum EShadowMapType : uint
    {
        SMT_None = 0,
        SMT_2D = 2,
    };
    public class FShadowMap
    {
        public readonly FGuid[] LightGuids;

        public FShadowMap(FAssetArchive Ar)
        {
            LightGuids = Ar.ReadArray<FGuid>();
        }
    }

    public class FShadowMap2D : FShadowMap
    {
        public readonly FPackageIndex Texture;
        public readonly FVector2D CoordinateScale;
        public readonly FVector2D CoordinateBias;
        public readonly bool[] bChannelValid;
        public readonly FVector4 InvUniformPenumbraSize;

        public FShadowMap2D(FAssetArchive Ar) : base(Ar)
        {
            Texture = new FPackageIndex(Ar);
            CoordinateScale = new FVector2D(Ar);
            CoordinateBias = new FVector2D(Ar);
            bChannelValid = Ar.ReadArray(4, () => Ar.ReadBoolean());

            if (Ar.Ver >= EUnrealEngineObjectUE4Version.STATIC_SHADOWMAP_PENUMBRA_SIZE)
            {
                InvUniformPenumbraSize = Ar.Read<FVector4>();
            }
            else
            {
                const float LegacyValue = 1.0f / .05f;
                InvUniformPenumbraSize = new FVector4(LegacyValue, LegacyValue, LegacyValue, LegacyValue);
            }
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    public readonly struct FPerInstanceLightmapData
    {
        public readonly FVector2D LightmapUVBias;
        public readonly FVector2D ShadowmapUVBias;
    }
}
