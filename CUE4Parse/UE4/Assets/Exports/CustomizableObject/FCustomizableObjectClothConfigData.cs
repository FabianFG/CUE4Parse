using CUE4Parse.UE4.Objects.UObject;
using CUE4Parse.UE4.Readers;

namespace CUE4Parse.UE4.Assets.Exports.CustomizableObject;

public struct FCustomizableObjectClothConfigData(FArchive Ar)
{
    public string ClassPath = Ar.ReadFString();
    public FName ConfigName = Ar.ReadFName();
    public byte[] ConfigBytes = Ar.ReadArray<byte>();
}