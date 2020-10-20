using CUE4Parse.UE4.Assets.Objects;
using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Versions;

namespace CUE4Parse.UE4.Assets.Exports.Textures
{
    public class FTexture2DMipMap
    {
        public readonly FByteBulkData Data;
        public readonly int SizeX;
        public readonly int SizeY;
        public readonly int SizeZ;

        public FTexture2DMipMap(FAssetArchive Ar)
        {
            SizeZ = 1;
            var cooked = Ar.Ver >= UE4Version.VER_UE4_TEXTURE_SOURCE_ART_REFACTOR && Ar.ReadBoolean();
            
            Data = new FByteBulkData(Ar);

            SizeX = Ar.Read<int>();
            SizeY = Ar.Read<int>();
            if (Ar.Game >= EGame.GAME_UE4_20)
            {
                SizeZ = Ar.Read<int>();    
            }

            if (Ar.Ver >= UE4Version.VER_UE4_TEXTURE_DERIVED_DATA2 && !cooked)
            {
                var derivedDataKey = Ar.ReadFString();
            }
        }
    }
}