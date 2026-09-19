using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Objects.UObject;
using CUE4Parse.UE4.Versions;
using Newtonsoft.Json;

namespace CUE4Parse.UE4.Assets.Exports.Engine.Font;

public class UFont : UObject
{
    private FFontPage[]? Pages;
    private int? CharactersPerPage;
    public Dictionary<ushort, ushort>? CharRemap;

    public override void Deserialize(FAssetArchive Ar, long validPos)
    {
        base.Deserialize(Ar, validPos);

        if (Ar.Ver < EUnrealEngineObjectUE3Version.Release122)
        {
            Pages = Ar.ReadArray(() => new FFontPage(Ar));
            CharactersPerPage = Ar.Read<int>();
        }
        else if (Ar.Ver < EUnrealEngineObjectUE3Version.FIXED_FONTS_SERIALIZATION)
        {
            var characters = Ar.ReadArray(() => new FFontCharacter(Ar));
            var textures = Ar.ReadArray(() => new FPackageIndex(Ar));
            Pages = [new FFontPage(textures, characters)];
        }

        if (Ar.Ver >= EUnrealEngineObjectUE3Version.Release119 && Ar.Ver < EUnrealEngineObjectUE3Version.FIXED_FONTS_SERIALIZATION)
        {
            Ar.Read<int>(); // Kerning
        }

        if (Ar.Ver >= EUnrealEngineObjectUE3Version.Release69)
        {
            CharRemap = Ar.ReadMap(Ar.Read<ushort>, Ar.Read<ushort>);

            if (Ar.Ver < EUnrealEngineObjectUE3Version.FIXED_FONTS_SERIALIZATION && Ar.Game < GAME_UE4_0)
            {
                Ar.ReadBoolean(); // IsRemapped
            }
        }

        if (Pages?.Length == 0 && CharactersPerPage == 0)
        {
            Ar.SkipFString(); // FontName
            Ar.Read<int>(); // FontHeight
        }
    }

    protected internal override void WriteJson(JsonWriter writer, JsonSerializer serializer)
    {
        base.WriteJson(writer, serializer);

        writer.WritePropertyName(nameof(CharactersPerPage));
        serializer.Serialize(writer, CharactersPerPage);

        if (Pages is {Length: > 0})
        {
            writer.WritePropertyName(nameof(Pages));
            serializer.Serialize(writer, Pages);
        }
    }
}
