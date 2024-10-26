using CUE4Parse.UE4.Assets.Readers;
using FIntVector2 = CUE4Parse.UE4.Objects.Core.Math.TIntVector2<int>;

namespace CUE4Parse.UE4.Assets.Exports.CustomizableObject.Mutable.Layouts;

public class Layout : IMutablePtr
{
    public FIntVector2 Size;
    public FLayoutBlock[] Blocks;
    public FIntVector2 MaxSize;
    public EPackStrategy Strategy;
    public EReductionMethod ReductionMethod;
    
    public bool IsBroken { get; set; }
    
    public Layout(FAssetArchive Ar)
    {
        Size = Ar.Read<FIntVector2>();
        Blocks = Ar.ReadArray(() => new FLayoutBlock(Ar));
        MaxSize = Ar.Read<FIntVector2>();
        Strategy = Ar.Read<EPackStrategy>();
        ReductionMethod = Ar.Read<EReductionMethod>();
    }
}

public enum EPackStrategy : uint
{
    Resizeable,
    Fixed,
    Overlay
}

public enum EReductionMethod : uint
{
    Halve,	// Divide axis by 2
    Unitary	// Reduces 1 block the axis 
}