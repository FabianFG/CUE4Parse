using CUE4Parse.UE4.Assets.Readers;

namespace CUE4Parse.UE4.Assets.Exports.CustomizableObject;

public class FClothingStreamable
{
    public int ClothingAssetIndex;
    public int ClothingAssetLOD;
    public int PhysicsAssetIndex;
    public uint Size = 0;
    public FMutableStreamableBlock Block;
    public uint SourceId = 0;

    public FClothingStreamable(FAssetArchive Ar)
    {
        ClothingAssetIndex = Ar.Read<int>();
        ClothingAssetLOD = Ar.Read<int>();
        PhysicsAssetIndex = Ar.Read<int>();
        Size = Ar.Read<uint>();
        Block = new FMutableStreamableBlock(Ar);
        SourceId = Ar.Read<uint>();
    }
}
