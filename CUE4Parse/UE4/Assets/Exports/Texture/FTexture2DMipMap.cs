using System;
using System.Collections.Generic;
using System.Linq;
using CUE4Parse.UE4.Assets.Exports.Component.Landscape;
using CUE4Parse.UE4.Assets.Objects;
using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Versions;
using Newtonsoft.Json;

namespace CUE4Parse.UE4.Assets.Exports.Texture;

[JsonConverter(typeof(FTexture2DMipMapConverter))]
public class FTexture2DMipMap
{
    public readonly FByteBulkData? BulkData;
    public int SizeX;
    public int SizeY;
    public int SizeZ;

    public FTexture2DMipMap(FByteBulkData bulkData, int sizeX, int sizeY, int sizeZ)
    {
        BulkData = bulkData;
        SizeX = sizeX;
        SizeY = sizeY;
        SizeZ = sizeZ;
    }

    public FTexture2DMipMap(FAssetArchive Ar, bool bSerializeMipData = true)
    {
        var cooked = Ar.Ver >= EUnrealEngineObjectUE4Version.TEXTURE_SOURCE_ART_REFACTOR && Ar.Game < EGame.GAME_UE5_0 ? Ar.ReadBoolean() : Ar.IsFilterEditorOnly;

        if (bSerializeMipData) BulkData = new FByteBulkData(Ar);

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

    public bool EnsureValidBulkData(IEnumerable<UTextureAllMipDataProviderFactory> provider/*, int index*/)
    {
        if (BulkData?.Data != null) return true;

        // we match mip by Sizes, maybe consider using indices at some point?
        switch (provider.FirstOrDefault()) // TODO: find better way
        {
            case ULandscapeTextureStorageProviderFactory landscapeProvider:
            {
                var mip = landscapeProvider.Mips.First(x => x.SizeX == SizeX && x.SizeY == SizeY);
                // decompress here and put that in BulkData.Data
                return true;
            }
            default: throw new NotImplementedException("unknown mip data provider");
        }
    }
}
