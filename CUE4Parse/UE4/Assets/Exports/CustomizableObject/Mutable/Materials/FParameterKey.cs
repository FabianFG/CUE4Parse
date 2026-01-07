using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Objects.UObject;

namespace CUE4Parse.UE4.Assets.Exports.CustomizableObject.Mutable.Materials;

public class FParameterKey
{
    public FName ParameterName;
    public int LayerIndex;

    public FParameterKey(FMutableArchive Ar)
    {
        ParameterName = Ar.ReadFName();
        LayerIndex = Ar.Read<int>();
    }
}