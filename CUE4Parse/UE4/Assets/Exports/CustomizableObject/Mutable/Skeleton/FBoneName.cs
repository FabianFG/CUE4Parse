using CUE4Parse.UE4.Readers;

namespace CUE4Parse.UE4.Assets.Exports.CustomizableObject.Mutable.Skeleton;

public class FBoneName
{
    public uint Id;

    public FBoneName(FArchive Ar)
    {
        Id = Ar.Read<uint>();
    }
}
