using CUE4Parse.UE4.Assets.Objects;
using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Objects.Engine;

namespace CUE4Parse.UE4.Assets.Exports.CriWare;

public class UAtomSoundBase : UObject
{
    public FByteBulkData? RawSnapshot;
    public int RawSnapshotNumChannels;

    public override void Deserialize(FAssetArchive Ar, long validPos)
    {
        base.Deserialize(Ar, validPos);

        var stripFlags = new FStripDataFlags(Ar);
        if (!Ar.IsFilterEditorOnly && !stripFlags.IsEditorDataStripped())
        {
            RawSnapshot = new FByteBulkData(Ar);
            RawSnapshotNumChannels = Ar.Read<int>();
        }
    }
}
