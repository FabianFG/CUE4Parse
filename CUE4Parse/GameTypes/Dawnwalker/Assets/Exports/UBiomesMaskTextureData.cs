using CUE4Parse.UE4.Assets.Exports;
using CUE4Parse.UE4.Assets.Readers;

namespace CUE4Parse.GameTypes.Dawnwalker.Assets.Exports;

public class UBiomesMaskTextureData : UObject
{
    public Lazy<byte[]> MaskData;

    public override void Deserialize(FAssetArchive Ar, long validPos)
    {
        base.Deserialize(Ar, validPos);
        _ = Ar.ReadBoolean();
        var size = Ar.Read<long>();
        var pngStartPos = Ar.Position;
        MaskData = new Lazy<byte[]>(() => Ar.ReadBytesAt(pngStartPos, (int)size));
        Ar.Position += size + 8;
    }
}
