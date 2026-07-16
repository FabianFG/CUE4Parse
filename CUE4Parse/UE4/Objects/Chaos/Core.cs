using CUE4Parse.UE4.Readers;

namespace CUE4Parse.UE4.Assets.Exports.Chaos;

// double
// 	FAABB3 = TAABB<FReal, 3>;
public class FAABB3 : TAABB<double>
{
    public FAABB3() : base(3, 0) { }

    public FAABB3(TAABB<double> box) : base(3, 0)
    {
        Min = box.Min;
        Max = box.Max;
    }
    
    public FAABB3(FArchive Ar) : base(Ar, 3)
    {
     
    }
}
