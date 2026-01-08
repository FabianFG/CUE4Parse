using CUE4Parse.UE4.Assets.Exports.CustomizableObject.Mutable.Images;
using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Objects.Core.Math;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace CUE4Parse.UE4.Assets.Exports.CustomizableObject.Mutable.Mesh.Layout;

public class FLayout
{
    public FIntVector2 Size;
    public FIntVector2 MaxSize;
    public EPackStrategy Strategy;
    public EReductionMethod ReductionMethod;
    public FLayoutBlock[] Blocks;
    public FImage[] Masks;
    
    public FLayout(FMutableArchive Ar)
    {
        Size = Ar.Read<FIntVector2>();
        MaxSize = Ar.Read<FIntVector2>();
        Strategy = Ar.Read<EPackStrategy>();
        ReductionMethod = Ar.Read<EReductionMethod>();
        Blocks = Ar.ReadArray<FLayoutBlock>();
        Masks = Ar.ReadPtrArray(() => new FImage(Ar));
    }
}

[JsonConverter(typeof(StringEnumConverter))]
public enum EPackStrategy : uint
{
    Resizeable,
    Fixed,
    Overlay
}

[JsonConverter(typeof(StringEnumConverter))]
public enum EReductionMethod : uint
{
    Halve,	// Divide axis by 2
    Unitary	// Reduces 1 block the axis 
}
