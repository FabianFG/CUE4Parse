using System.Collections.Generic;
using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Objects.Core.Math;

namespace CUE4Parse.UE4.Assets.Exports.CustomizableObject.Mutable.Materials;

public class FMaterial
{
    public Dictionary<FParameterKey, FImageParameterData> ImageParameters;
    public Dictionary<FParameterKey, FVector4> ColorParameters;
    public Dictionary<FParameterKey, float> ScalarParameters;
    
    public FMaterial(FMutableArchive Ar)
    {
        ImageParameters = Ar.ReadMap(() => new FParameterKey(Ar), () => new FImageParameterData(Ar));
        ColorParameters = Ar.ReadMap(() => new FParameterKey(Ar), Ar.Read<FVector4>);
        ScalarParameters = Ar.ReadMap(() => new FParameterKey(Ar), Ar.Read<float>);
    }
}