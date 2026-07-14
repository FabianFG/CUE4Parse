using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Objects.Core.Math;
using CUE4Parse.UE4.Versions;

namespace CUE4Parse.UE4.Assets.Exports.CustomizableObject.Mutable.Materials;

public class FMaterial
{
    public int ReferenceID = -1;
    public Dictionary<FParameterKey, FImageParameterData> ImageParameters;
    public Dictionary<FParameterKey, FVector4> ColorParameters;
    public Dictionary<FParameterKey, float> ScalarParameters;

    public FMaterial(FMutableArchive Ar)
    {
        ReferenceID = Ar.Game < GAME_UE5_8 ? Ar.Read<int>() : -1;
        ImageParameters = Ar.ReadMap(() => new FParameterKey(Ar), () => new FImageParameterData(Ar));
        ColorParameters = Ar.ReadMap(() => new FParameterKey(Ar), Ar.Read<FVector4>);
        ScalarParameters = Ar.ReadMap(() => new FParameterKey(Ar), Ar.Read<float>);
    }
}
