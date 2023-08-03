using System;
using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Objects.Core.Math;
using CUE4Parse.UE4.Objects.Core.Misc;
using CUE4Parse.UE4.Objects.Engine;
using CUE4Parse.UE4.Versions;
using Serilog;

namespace CUE4Parse.UE4.Assets.Exports.Texture;

public class UTexture2D : UTexture
{
    public FIntPoint ImportedSize { get; private set; }

    public override void Deserialize(FAssetArchive Ar, long validPos)
    {
        base.Deserialize(Ar, validPos);
        ImportedSize = GetOrDefault<FIntPoint>(nameof(ImportedSize));

        var stripDataFlags = Ar.Read<FStripDataFlags>();
        var bCooked = Ar.Ver >= EUnrealEngineObjectUE4Version.ADD_COOKED_TO_TEXTURE2D && Ar.ReadBoolean();
        if (Ar.Ver < EUnrealEngineObjectUE4Version.TEXTURE_SOURCE_ART_REFACTOR)
        {
            Log.Warning("Untested code: UTexture2D::LegacySerialize");
            // https://github.com/EpicGames/UnrealEngine/blob/2092a941a52c55750072f24cd4757176dfaa8326/Engine/Source/Runtime/Engine/Private/Texture2D.cpp

            var legacyMips = Array.Empty<FTexture2DMipMap>();

            var bHasLegacyMips = GetOrDefault("bDisableDerivedDataCache_DEPRECATED", false);
            if (bHasLegacyMips)
            {
                legacyMips = Ar.ReadArray(() => new FTexture2DMipMap(Ar));
            }

            var textureFileCacheGuidDeprecated = Ar.Read<FGuid>();

            Format = GetOrDefault(nameof(Format), EPixelFormat.PF_Unknown);

            if (bHasLegacyMips && legacyMips.Length > 0)
            {
                // TODO: Populate PlatformData.Mips[] with LegacyMips data.
            }
        }

        if (bCooked)
        {
            var bSerializeMipData = true;

            if (Ar.Game >= EGame.GAME_UE5_3)
            {
                // Controls whether FByteBulkData is serialized??
                bSerializeMipData = Ar.ReadBoolean();
            }

            DeserializeCookedPlatformData(Ar, bSerializeMipData);
        }
    }
}
