using CUE4Parse.UE4.Readers;

namespace CUE4Parse.UE4.Assets.Exports.Rig;

public class RawActivationFunction
{
    public ushort FunctionId; // cld be EActivationFunction
    public float[] Parameters;

    public RawActivationFunction(FArchiveBigEndian Ar)
    {
        FunctionId = Ar.Read<ushort>();
        Parameters = Ar.ReadArray<float>();
    }
}
