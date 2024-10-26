using CUE4Parse.UE4.Assets.Readers;

namespace CUE4Parse.UE4.Assets.Exports.CustomizableObject.Mutable.Skeletons;

public class FBoneName
{
    public uint Id;
    
    public FBoneName(FAssetArchive Ar)
    {
        Id = Ar.Read<uint>();
    }

    public FBoneName(uint id)
    {
        Id = id;
    }
}