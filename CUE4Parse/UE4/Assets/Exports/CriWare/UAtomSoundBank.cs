using CUE4Parse.UE4.Assets.Objects;
using CUE4Parse.UE4.Assets.Readers;

namespace CUE4Parse.UE4.Assets.Exports.CriWare;

public class UAtomSoundBank : UObject
{
    public FByteBulkData? RawData;

    public override void Deserialize(FAssetArchive Ar, long validPos)
    {
        base.Deserialize(Ar, validPos);

        var bCooked = Ar.ReadBoolean();
        if (bCooked) RawData = new FByteBulkData(Ar);
    }
}

