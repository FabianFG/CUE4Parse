using CUE4Parse.UE4.Assets.Exports.Material;
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
            // https://github.com/gildor2/UEViewer/blob/master/Unreal/UnrealMaterial/UnTexture4.cpp#L166
            // This code lives in UTexture2D::LegacySerialize(). It relies on some deprecated properties, and modern
            // code UE4 can't read cooked packages prepared with pre-VER_UE4_TEXTURE_SOURCE_ART_REFACTOR version of
            // the engine. So, it's not possible to know what should happen there unless we'll get some working game
            // which uses old UE4 version.bDisableDerivedDataCache_DEPRECATED in UE4 serialized as property, when set
            // to true - has serialization of TArray<FTexture2DMipMap>. We suppose here that it's 'false'.
            var textureFileCacheGuidDeprecated = Ar.Read<FGuid>();
        }

        if (bCooked)
        {
            DeserializeCookedPlatformData(Ar);
        }
    }
}
