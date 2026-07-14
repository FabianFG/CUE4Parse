using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Objects.UObject;
using CUE4Parse.UE4.Versions;

namespace CUE4Parse.UE4.Assets.Exports.CustomizableObject.Mutable.Materials;

public class FParameterKey
{
    public FName ParameterName;
    public int LayerIndex;

    public FParameterKey(FMutableArchive Ar)
    {
        ParameterName = Ar.ReadFName();
        LayerIndex = Ar.Game >= GAME_UE5_8 ? Ar.Read<int>() : 0;
    }
}
