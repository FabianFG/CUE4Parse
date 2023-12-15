using CUE4Parse.UE4.Writers;

namespace CUE4Parse_Conversion.Animations.UEFormat;

public class FFloatKey : FAnimKey<float>
{
    public FFloatKey(int frame, float value) : base(frame, value) { }

    public override void Serialize(FArchiveWriter Ar)
    {
        base.Serialize(Ar);
        Ar.Write(Value);
    }
    
}