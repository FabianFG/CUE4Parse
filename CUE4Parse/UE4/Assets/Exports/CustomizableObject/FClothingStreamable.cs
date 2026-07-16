using CUE4Parse.UE4.Readers;

namespace CUE4Parse.UE4.Assets.Exports.CustomizableObject;

public readonly struct FClothingStreamable(FArchive Ar)
{
    public readonly int ClothingAssetIndex = Ar.Read<int>();
    public readonly int ClothingAssetLod = Ar.Read<int>();
    public readonly int PhysicsAssetIndex = Ar.Read<int>();
    public readonly uint Size = Ar.Read<uint>();
    public readonly FMutableStreamableBlock Block = new FMutableStreamableBlock(Ar);
    public readonly uint SourceId = Ar.Read<uint>();
}
