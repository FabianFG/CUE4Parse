using CUE4Parse.UE4.Assets.Objects;
using CUE4Parse.UE4.Assets.Readers;

namespace CUE4Parse.UE4.Assets.Exports.Texture;

// RawData is a video in bnk format (https://www.radgametools.com/bnkdown.htm)
public class UTextureMovie : UTexture
{
    public override void Deserialize(FAssetArchive Ar, long validPos)
    {
        base.Deserialize(Ar, validPos);

        new FByteBulkData(Ar); // RawData
    }
}
