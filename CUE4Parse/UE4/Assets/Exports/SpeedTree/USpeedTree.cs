using CUE4Parse.UE4.Assets.Readers;

namespace CUE4Parse.UE4.Assets.Exports.SpeedTree;

public class USpeedTree : UObject
{
    public override void Deserialize(FAssetArchive Ar, long validPos)
    {
        base.Deserialize(Ar, validPos);

        // This can be skipped for now as this is a pretty long function
        var NumBytes = Ar.Read<int>();
        Ar.Position += NumBytes;
    }
};
