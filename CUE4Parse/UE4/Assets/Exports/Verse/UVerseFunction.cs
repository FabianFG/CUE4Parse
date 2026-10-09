using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Objects.UObject;
using Newtonsoft.Json;

namespace CUE4Parse.UE4.Assets.Exports.Verse;

public class UVerseFunction : UFunction
{
    public EVerseFunctionFlags VerseFunctionFlags;
    public FName AlternateName;
    //public FName CoercedOriginalName; // Added in https://github.com/EpicGames/UnrealEngine/commit/976d055b7c0f7bc19576d6896bfa6513a32efb7d#diff-a277a38206a06c667a114c7e039a4c3ce5a4ac91dbf0c7a82448d38925556533

    public override void Deserialize(FAssetArchive Ar, long validPos)
    {
        if (Ar.Game < GAME_UE6_0)
        {
            base.Deserialize(Ar, validPos);
            return;
        }

        VerseFunctionFlags = Ar.Read<EVerseFunctionFlags>();
        base.Deserialize(Ar, validPos);
        AlternateName = Ar.ReadFName();
        //CoercedOriginalName = Ar.ReadFName();
    }

    protected internal override void WriteJson(JsonWriter writer, JsonSerializer serializer)
    {
        base.WriteJson(writer, serializer);

        writer.WritePropertyName(nameof(VerseFunctionFlags));
        writer.WriteValue(VerseFunctionFlags);
        writer.WritePropertyName(nameof(AlternateName));
        writer.WriteValue(AlternateName);
    }

    public enum EVerseFunctionFlags : uint
    {
        None                             = 0x00000000u,
        UHTNative                        = 0x00000001u,
        UHTTaskUpdate                    = 0x00000002u,
        AccessibleFromEngineGameplay     = 0x00000004u,
        CanAccessEpicInternal            = 0x00000008u
    }
}
