using CUE4Parse.UE4.Objects.Core.Math;
using CUE4Parse.UE4.Readers;

namespace CUE4Parse.UE4.Assets.Exports.Groom;

public class FVector4_16 : IUStruct
{
    public FFloat16 X;
    public FFloat16 Y;
    public FFloat16 Z;
    public FFloat16 W;

    public FVector4_16(FArchive Ar)
    {
        X = new FFloat16(Ar);
        Y = new FFloat16(Ar);
        Z = new FFloat16(Ar);
        W = new FFloat16(Ar);
    }
}
