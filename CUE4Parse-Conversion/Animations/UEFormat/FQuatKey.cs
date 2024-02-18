using CUE4Parse.UE4.Objects.Core.Math;
using CUE4Parse.UE4.Writers;

namespace CUE4Parse_Conversion.Animations.UEFormat;

public class FQuatKey : FAnimKey<FQuat>
{
    public FQuatKey(int frame, FQuat value) : base(frame, value) { }

    public override void Serialize(FArchiveWriter Ar)
    {
        base.Serialize(Ar);
        Value.Serialize(Ar);
    }
    
}