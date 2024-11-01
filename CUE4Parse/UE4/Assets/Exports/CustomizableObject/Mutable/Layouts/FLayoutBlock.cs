using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Readers;
using FIntVector2 = CUE4Parse.UE4.Objects.Core.Math.TIntVector2<int>;

namespace CUE4Parse.UE4.Assets.Exports.CustomizableObject.Mutable.Layouts;

public class FLayoutBlock
{
    public FIntVector2 Min;
    public FIntVector2 Size;
    public ulong Id;
    public int Priority;
    public bool bReduceBothAxes;
    public bool bReduceByTwo;
    
    public FLayoutBlock(FArchive Ar)
    {
        Min = Ar.Read<FIntVector2>();
        Size = Ar.Read<FIntVector2>();
        Id = Ar.Read<ulong>();
        Priority = Ar.Read<int>();
        var flags = Ar.Read<int>();
        bReduceBothAxes = (flags & 1) == 1;
        bReduceByTwo = (flags & 2) == 2;
    }
}