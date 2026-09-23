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

        if (Ar.Game < GAME_UE4_0)
        {
            var structType = Class?.Super?.Name.Text ?? Class?.Name.Text;
            var pushedStructType = !string.IsNullOrEmpty(structType);
            if (pushedStructType) Ar.StructTypeStack.Push(structType!);
            try
            {
                DeserializePropertiesTagged(Properties, Ar, false);
            }
            finally
            {
                if (pushedStructType) Ar.StructTypeStack.Pop();
            }
        }
    }
}
