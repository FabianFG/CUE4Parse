using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using CUE4Parse.UE4.Assets.Readers;
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

            if (FRenderingObjectVersion.Get(Ar) >= FRenderingObjectVersion.Type.StoreReflectionCaptureBrightnessForCooking)
            {
                Brightness = Ar.Read<float>();
            }

            FullHDRCapturedData = Ar.ReadArray<byte>(); // Can also be stripped, but still a byte[]

            if (FMobileObjectVersion.Get(Ar) >= FMobileObjectVersion.Type.StoreReflectionCaptureCompressedMobile)
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

    public class FPrecomputedLightVolumeData
    {
        public FPrecomputedLightVolumeData? Volume;

        public FPrecomputedLightVolumeData(FArchive Ar)
        {
            var bValid = Ar.ReadBoolean();

            if (bValid)
            {
                Volume = new FPrecomputedLightVolumeData(Ar); // It serializes itself?
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
            Bounds = Ar.Read<FBox>();
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

        public FVolumetricLightmapDataLayer(FArchive Ar)
        {
            Data = Ar.ReadArray<byte>();
        }
    }

    public class FMeshMapBuildData
    {
        public FLightMap LightMap;
        public FLightMap ShadowMap; // Same LightGuids array
        public FGuid[] IrrelevantLights;
        public FPerInstanceLightmapData[] PerInstanceLightmapData;

        public FMeshMapBuildData(FArchive Ar)
        {
            LightMap = new FLightMap(Ar);
            ShadowMap = new FLightMap(Ar);
            IrrelevantLights = Ar.ReadArray<FGuid>();
            PerInstanceLightmapData = Ar.ReadBulkArray<FPerInstanceLightmapData>();
        }
    }

    public class FLightMap
    {
        public readonly FGuid[] LightGuids;

        public FLightMap(FArchive Ar)
        {
            LightGuids = Ar.ReadArray<FGuid>();
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    public readonly struct FPerInstanceLightmapData
    {
        public readonly FVector2D LightmapUVBias;
        public readonly FVector2D ShadowmapUVBias;
    }
}
