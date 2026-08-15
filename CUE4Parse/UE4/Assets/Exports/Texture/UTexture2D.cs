using CUE4Parse.UE4.Assets.Objects;
using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Objects.Core.Math;
using CUE4Parse.UE4.Objects.Core.Misc;
using CUE4Parse.UE4.Objects.Engine;
using CUE4Parse.UE4.Objects.UObject;
using CUE4Parse.UE4.Versions;

namespace CUE4Parse.UE4.Assets.Exports.Texture;

public class UTexture2D : UTexture
{

    public FIntPoint ImportedSize { get; private set; }
    public TextureAddress AddressX { get; private set; }
    public TextureAddress AddressY { get; private set; }
    public bool bForcePVRTC4 { get; private set; }
    public FName TextureFileCacheName { get; private set; }

    public override TextureAddress GetTextureAddressX() => AddressX;
    public override TextureAddress GetTextureAddressY() => AddressY;

    public override void Deserialize(FAssetArchive Ar, long validPos)
    {
        if (Ar.Game == GAME_WorldofJadeDynasty) Ar.Position += 12;
        base.Deserialize(Ar, validPos);
        ImportedSize = GetOrDefault<FIntPoint>(nameof(ImportedSize));
        AddressX = GetOrDefault<TextureAddress>(nameof(AddressX));
        AddressY = GetOrDefault<TextureAddress>(nameof(AddressY));
        bForcePVRTC4 = GetOrDefault<bool>(nameof(bForcePVRTC4));
        TextureFileCacheName = GetOrDefault<FName>(nameof(TextureFileCacheName));

        var stripDataFlags = new FStripDataFlags(Ar);
        var bCooked = Ar.Ver >= EUnrealEngineObjectUE4Version.ADD_COOKED_TO_TEXTURE2D && Ar.ReadBoolean();
        if (Ar.Ver < EUnrealEngineObjectUE4Version.TEXTURE_SOURCE_ART_REFACTOR)
        {
            if (Ar.Ver < EUnrealEngineObjectUE3Version.RENDERING_REFACTOR)
            {
                Ar.Position += sizeof(int) * 2; // int - SizeX, SizeY
                Format = (EPixelFormat)Ar.Read<int>();
            }

            var legacyMips = Array.Empty<FTexture2DMipMap>();

            var bHasLegacyMips =  Ar.Game < GAME_UE4_0 || GetOrDefault("bDisableDerivedDataCache_DEPRECATED", false);
            if (bHasLegacyMips)
            {
                legacyMips = Ar.ReadArray(() => TextureFileCacheName.IsNone ? new FTexture2DMipMap(Ar) : new FTexture2DMipMap(Ar, TextureFileCacheName.Text));
            }

            if (Ar.Ver >= EUnrealEngineObjectUE3Version.ADDED_TEXTURE_FILECACHE_GUIDS)
            {
                Ar.Position += sizeof(uint) * 4; // FGuid - TextureFileCacheGuid_DEPRECATED
            }

            if (Ar.Ver >= EUnrealEngineObjectUE3Version.ADDED_CACHED_IPHONE_DATA)
            {
                Ar.ReadArray(() => new FTexture2DMipMap(Ar)); // CachedPVRTCMips
            }

            if (Ar.Ver >= EUnrealEngineObjectUE3Version.VERSION_NUMBER_FIX_FOR_FLASH_TEXTURES)
            {
                Ar.Position += sizeof(int); // int - CachedFlashMipsMaxResolution
                Ar.ReadArray(() => new FTexture2DMipMap(Ar)); // CachedATITCMips
                new FByteBulkData(Ar); // CachedFlashMips
            }

            if (Ar.Ver >= EUnrealEngineObjectUE3Version.ANDROID_ETC_SEPARATED)
            {
                Ar.ReadArray(() => new FTexture2DMipMap(Ar)); // CachedETCMips
            }

            Format = GetOrDefault(nameof(Format), EPixelFormat.PF_Unknown);

            if (bHasLegacyMips && legacyMips.Length > 0)
            {
                PlatformData.Mips = legacyMips;

                /*
                 * Todo: add the extra android stuff needed
                 * Todo: Find a way to allow users to change Platform

                if (false) // if game is ios
                {
                    if (Format == EPixelFormat.PF_DXT1)
                    {
                        Format = bForcePVRTC4 ? EPixelFormat.PF_PVRTC4 : EPixelFormat.PF_PVRTC2;
                    }
                    else if (Format == EPixelFormat.PF_DXT5)
                    {
                        Format = EPixelFormat.PF_PVRTC4;
                    }
                } else if (false) // if game is android
                {
                    if (Format == EPixelFormat.PF_DXT1)
                    {
                        Format = EPixelFormat.PF_ETC1;
                    } else if (Format == EPixelFormat.PF_DXT5)
                    {
                        // unsupported RGBA4
                    }
                }*/

            }
        }

        if (bCooked)
        {
            var bSerializeMipData = true;
            if (Ar.Game >= GAME_UE5_3 || Ar.Game == GAME_TheFirstDescendant)
            {
                // Controls whether FByteBulkData
                bSerializeMipData = Ar.ReadBoolean();
            }

            if (Ar.Position >= validPos) return;

            DeserializeCookedPlatformData(Ar, bSerializeMipData);
        }
    }
}
