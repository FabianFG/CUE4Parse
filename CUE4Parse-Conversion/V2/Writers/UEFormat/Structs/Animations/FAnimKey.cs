using CUE4Parse.UE4.Writers;

namespace CUE4Parse_Conversion.V2.Writers.UEFormat.Structs.Animations;

public class FAnimKey<T> : ISerializable
{
    public readonly int Frame;
    public T Value;

    public FAnimKey(int frame, T value)
    {
        Frame = frame;
        Value = value;
    }

    public virtual void Serialize(FArchiveWriter Ar)
    {
        Ar.Write(Frame);
    }
}
