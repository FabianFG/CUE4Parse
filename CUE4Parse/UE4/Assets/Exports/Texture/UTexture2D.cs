using System;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using CUE4Parse.UE4.Assets.Exports.Material;
using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Objects.Core.Math;
using CUE4Parse.UE4.Objects.Core.Misc;
using CUE4Parse.UE4.Objects.Engine;
using CUE4Parse.UE4.Objects.UObject;
using CUE4Parse.UE4.Versions;
using Newtonsoft.Json;
using Serilog;

namespace CUE4Parse.UE4.Assets.Exports.Texture
{
    public class UTexture2D : UTexture
    {
        public int SizeX { get; private set; }
        public int SizeY { get; private set; }
        public int PackedData { get; private set; } // important only while UTextureCube4 is derived from UTexture2D in out implementation
        public EPixelFormat Format { get; private set; } = EPixelFormat.PF_Unknown;
        public FOptTexturePlatformData OptData { get; private set; }
        public int FirstMipToSerialize { get; private set; }
        public FTexture2DMipMap[] Mips { get; private set; }
        public FVirtualTextureBuiltData? VTData { get; private set; }
        public bool IsVirtual => VTData != null;
        public FIntPoint ImportedSize { get; private set; }
        public bool bRenderNearestNeighbor { get; private set; }
        public bool isNormalMap { get; private set; }

        public override void Deserialize(FAssetArchive Ar, long validPos)
        {
            base.Deserialize(Ar, validPos);
            ImportedSize = GetOrDefault<FIntPoint>(nameof(ImportedSize));
            if (TryGetValue(out FName trigger, "LODGroup", "Filter") && !trigger.IsNone)
                bRenderNearestNeighbor = trigger.Text.EndsWith("TEXTUREGROUP_Pixels2D", StringComparison.OrdinalIgnoreCase) ||
                                         trigger.Text.EndsWith("TF_Nearest", StringComparison.OrdinalIgnoreCase);
            if (TryGetValue(out FName normalTrigger, "CompressionSettings") && !normalTrigger.IsNone)
                isNormalMap = normalTrigger.Text.EndsWith("TC_Normalmap", StringComparison.OrdinalIgnoreCase);

            var stripDataFlags = Ar.Read<FStripDataFlags>();
            var bCooked = Ar.Ver >= EUnrealEngineObjectUE4Version.ADD_COOKED_TO_TEXTURE2D && Ar.ReadBoolean();
            if (Ar.Ver < EUnrealEngineObjectUE4Version.TEXTURE_SOURCE_ART_REFACTOR)
            {
                Log.Warning("Untested code: UTexture2D::LegacySerialize");
                // https://github.com/gildor2/UEViewer/blob/master/Unreal/UnrealMaterial/UnTexture4.cpp#L166
                // This code lives in UTexture2D::LegacySerialize(). It relies on some deprecated properties, and modern
                // code UE4 can't read cooked packages prepared with pre-VER_UE4_TEXTURE_SOURCE_ART_REFACTOR version of
                // the engine. So, it's not possible to know what should happen there unless we'll get some working game
                // which uses old UE4 version.bDisableDerivedDataCache_DEPRECATED in UE4 serialized as property, when set
                // to true - has serialization of TArray<FTexture2DMipMap>. We suppose here that it's 'false'.
                var textureFileCacheGuidDeprecated = Ar.Read<FGuid>();
            }

            // Formats are added in UE4 in Source/Developer/<Platform>TargetPlatform/Private/<Platform>TargetPlatform.h,
            // in TTargetPlatformBase::GetTextureFormats(). Formats are choosen depending on platform settings (for example,
            // for Android) or depending on UTexture::CompressionSettings. Windows, Linux and Mac platform uses the same
            // texture format (see FTargetPlatformBase::GetDefaultTextureFormatName()). Other platforms uses different formats,
            // but all of them (except Android) uses single texture format per object.

            if (bCooked)
            {
                var pixelFormatEnum = Ar.ReadFName();
                while (!pixelFormatEnum.IsNone)
                {
                    var skipOffset = Ar.Game switch
                    {
                        >= EGame.GAME_UE5_0 => Ar.AbsolutePosition + Ar.Read<long>(),
                        >= EGame.GAME_UE4_20 => Ar.Read<long>(),
                        _ => Ar.Read<int>()
                    };

                    var pixelFormat = EPixelFormat.PF_Unknown;
                    Enum.TryParse(pixelFormatEnum.Text, out pixelFormat);

                    if (Format == EPixelFormat.PF_Unknown)
                    {
                        //?? check whether we can support this pixel format
#if DEBUG
                        Log.Debug($"Loading data for format {pixelFormatEnum}");
#endif
                        var data = new FTexturePlatformData(Ar);

                        if (Ar.Game == EGame.GAME_SeaOfThieves)
                        {
                            Ar.Position += 4;
                        }
                        
                        if (Ar.AbsolutePosition != skipOffset)
                        {
                            Log.Warning(
                                $"Texture2D read incorrectly. Offset {Ar.AbsolutePosition}, Skip Offset {skipOffset}, Bytes remaining {skipOffset - Ar.AbsolutePosition}");
                            Ar.SeekAbsolute(skipOffset, SeekOrigin.Begin);
                        }

                        // copy data to UTexture2D
                        SizeX = data.SizeX;
                        SizeY = data.SizeY;
                        PackedData = data.PackedData;
                        Format = pixelFormat;
                        OptData = data.OptData;
                        FirstMipToSerialize = data.FirstMipToSerialize;
                        Mips = data.Mips;
                        VTData = data.VTData;
                    }
                    else
                    {
#if DEBUG
                        Log.Debug($"Skipping data for format {pixelFormatEnum}");
#endif
                        Ar.SeekAbsolute(skipOffset, SeekOrigin.Begin);
                    }
                    // read next format name
                    pixelFormatEnum = Ar.ReadFName();
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public FTexture2DMipMap? GetFirstMip() => Mips.FirstOrDefault(x => x.Data.Data != null);

        public override void GetParams(CMaterialParams parameters)
        {
            // ???
        }

        protected internal override void WriteJson(JsonWriter writer, JsonSerializer serializer)
        {
            base.WriteJson(writer, serializer);

            writer.WritePropertyName("SizeX");
            writer.WriteValue(SizeX);

            writer.WritePropertyName("SizeY");
            writer.WriteValue(SizeY);

            writer.WritePropertyName("PackedData");
            writer.WriteValue(PackedData);

            writer.WritePropertyName("PixelFormat");
            writer.WriteValue(Format.ToString());

            if (OptData.ExtData != 0 && OptData.NumMipsInTail != 0)
            {
                writer.WritePropertyName("OptData");
                serializer.Serialize(writer, OptData);
            }

            writer.WritePropertyName("FirstMipToSerialize");
            writer.WriteValue(FirstMipToSerialize);

            if (Mips is { Length: > 0 })
            {
                writer.WritePropertyName("Mips");
                serializer.Serialize(writer, Mips);
            }

            if (VTData != null)
            {
                writer.WritePropertyName("VTData");
                writer.WriteValue(VTData);
            }
        }
    }
}