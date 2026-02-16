using CUE4Parse.UE4.Assets.Exports.BuildData;
using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Objects.Core.Misc;
using CUE4Parse.UE4.Readers;
using CUE4Parse.UE4.Versions;
using Newtonsoft.Json;

namespace CUE4Parse.UE4.Assets.Exports.Component;

public class UReflectionCaptureComponent : USceneComponent
{
    public bool bLegacy = false;
    public FGuid? SavedVersion;
    public float AverageBrightness = 1.0f;
    public FReflectionCaptureMapBuildData? LegacyMapBuildData;

    override public void Deserialize(FAssetArchive Ar, long validPos)
    {
        base.Deserialize(Ar, validPos);

        if (FReflectionCaptureObjectVersion.Get(Ar) < FReflectionCaptureObjectVersion.Type.MoveReflectionCaptureDataToMapBuildData)
        {
            if (Ar.Ver >= EUnrealEngineObjectUE4Version.REFLECTION_CAPTURE_COOKING)
                bLegacy = Ar.ReadBoolean();

            if (Ar.Ver >= EUnrealEngineObjectUE4Version.REFLECTION_DATA_IN_PACKAGES)
            {
                if (Ar.Game >= EGame.GAME_UE4_19)
                {
                    SavedVersion = Ar.Read<FGuid>();
                    if (FRenderingObjectVersion.Get(Ar) >= FRenderingObjectVersion.Type.ReflectionCapturesStoreAverageBrightness)
                        AverageBrightness = Ar.Read<float>();

                    var EndOffset = Ar.Read<int>();
                    FGuid LegacyReflectionCaptureVer = new(0x0c669396, 0x9cb849ae, 0x9f4120ff, 0x5812f4d3);
                    if (SavedVersion != LegacyReflectionCaptureVer)
                    {
                        Ar.Position = EndOffset;
                    }
                    else
                    {
                        if (!Ar.ReadBoolean())  return;

                        LegacyMapBuildData = new FReflectionCaptureMapBuildData();
                        if (FRenderingObjectVersion.Get(Ar) >= FRenderingObjectVersion.Type.CustomReflectionCaptureResolutionSupport)
                        {
                            LegacyMapBuildData.CubemapSize = Ar.Read<int>();
                        }
                        else
                        {
                            LegacyMapBuildData.CubemapSize = 128;
                            var CompressedCapturedData = Ar.ReadArray<byte>();
                            if (CompressedCapturedData.Length <= 0) return;
                            var byteAr = new FByteArchive("CompressedCapturedData", CompressedCapturedData, Ar.Versions);
                            var uncompressedSize = byteAr.Read<int>();
                            var compressedSize = byteAr.Read<int>();

                            LegacyMapBuildData.FullHDRCapturedData = Compression.Compression.Decompress(byteAr.ReadArray<byte>(compressedSize), uncompressedSize, Compression.CompressionMethod.Zlib);
                        }

                        LegacyMapBuildData.AverageBrightness = AverageBrightness;
                    }
                }
                else
                {
                    if (bLegacy)
                    {
                        if (Ar.Game >= EGame.GAME_UE4_14)
                            AverageBrightness = Ar.Read<float>();
                        var formatsCount = Ar.Read<int>();
                        for (var i = 0; i < formatsCount; i++)
                        {
                            var format = Ar.ReadFName();
                            if (format.Text == "FullHDR")
                            {
                                if (Ar.ReadBoolean())
                                {
                                    LegacyMapBuildData = new FReflectionCaptureMapBuildData();
                                    LegacyMapBuildData.CubemapSize = Ar.Read<int>();
                                    LegacyMapBuildData.FullHDRCapturedData = Ar.ReadArray<byte>();
                                }
                            }
                            else if (Ar.ReadBoolean())
                                Ar.SkipFixedArray(1); // EncodedHDRData
                        }
                    }
                }
            }
        }
    }

    protected internal override void WriteJson(JsonWriter writer, JsonSerializer serializer)
    {
        base.WriteJson(writer, serializer);
        writer.WritePropertyName(nameof(bLegacy));
        writer.WriteValue(bLegacy);
        writer.WritePropertyName(nameof(SavedVersion));
        serializer.Serialize(writer, SavedVersion);
        writer.WritePropertyName(nameof(AverageBrightness));
        writer.WriteValue(AverageBrightness);
        if (LegacyMapBuildData is not null)
        {
            writer.WritePropertyName(nameof(LegacyMapBuildData));
            serializer.Serialize(writer, LegacyMapBuildData);
        }
    }
}
