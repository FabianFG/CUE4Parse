using CUE4Parse.UE4.Assets.Objects;
using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Versions;
using Newtonsoft.Json;

namespace CUE4Parse.UE4.Assets.Exports.Texture;

[JsonConverter(typeof(FTexture2DMipMapConverter))]
public class FTexture2DMipMap
{
    public readonly FByteBulkData BulkData;
    public int SizeX;
    public int SizeY;
    public int SizeZ;

    public FTexture2DMipMap(FAssetArchive Ar)
    {
        var cooked = Ar.Ver >= EUnrealEngineObjectUE4Version.TEXTURE_SOURCE_ART_REFACTOR && Ar.Game < EGame.GAME_UE5_0 ? Ar.ReadBoolean() : Ar.IsFilterEditorOnly;

        BulkData = new FByteBulkData(Ar);

        if (Ar.Game == EGame.GAME_Borderlands3)
        {
            SizeX = Ar.Read<ushort>();
            SizeY = Ar.Read<ushort>();
            SizeZ = Ar.Read<ushort>();
        }
        else
        {
            SizeX = Ar.Read<int>();
            SizeY = Ar.Read<int>();
            SizeZ = Ar.Game >= EGame.GAME_UE4_20 ? Ar.Read<int>() : 1;
        }

        if (Ar.Ver >= EUnrealEngineObjectUE4Version.TEXTURE_DERIVED_DATA2 && !cooked)
        {
            var derivedDataKey = Ar.ReadFString();
        }
    }
}
