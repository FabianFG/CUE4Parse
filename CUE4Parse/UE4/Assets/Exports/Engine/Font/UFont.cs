using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Objects.UObject;
using CUE4Parse.UE4.Versions;

namespace CUE4Parse.UE4.Assets.Exports.Engine.Font;

public class UFont : UObject
{
    public Dictionary<ushort, ushort> CharRemap;

    public override void Deserialize(FAssetArchive Ar, long validPos)
    {
        base.Deserialize(Ar, validPos);
        if (Ar.Ver < EUnrealEngineObjectUE3Version.FIXED_FONTS_SERIALIZATION)
        {
            Ar.ReadArray(() => new FFontCharacter(Ar)); // Characters
            Ar.ReadArray(() => new FPackageIndex(Ar)); // Textures
        }

        if (Ar.Ver >= EUnrealEngineObjectUE3Version.Release119 && Ar.Ver < EUnrealEngineObjectUE3Version.FIXED_FONTS_SERIALIZATION)
        {
            Ar.Read<int>(); // Kerning
        }

        if (Ar.Ver >= EUnrealEngineObjectUE3Version.Release69)
        {
            CharRemap = Ar.ReadMap(Ar.Read<ushort>, Ar.Read<ushort>);

            if (Ar.Ver < EUnrealEngineObjectUE3Version.FIXED_FONTS_SERIALIZATION && Ar.Game < EGame.GAME_UE4_0)
            {
                Ar.ReadBoolean(); // IsRemapped
            }
        }
    }
}
