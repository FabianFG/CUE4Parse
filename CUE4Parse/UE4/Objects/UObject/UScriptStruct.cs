using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Versions;

namespace CUE4Parse.UE4.Objects.UObject;

public class UScriptStruct : UStruct
{
    public EStructFlags StructFlags;

    public override void Deserialize(FAssetArchive Ar, long validPos)
    {
        base.Deserialize(Ar, validPos);
        if (Ar.Ver >= EUnrealEngineObjectUE3Version.LIGHTING_CHANNEL_SUPPORT)
        {
            StructFlags = Ar.Read<EStructFlags>();
        }

        if (Ar.Game < EGame.GAME_UE4_0)
        {
            DeserializePropertiesTagged(Properties, Ar, false);
        }
    }
}
