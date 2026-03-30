using CUE4Parse.UE4.Assets.Exports.CustomizableObject.Mutable.Images;
using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Objects.Core.Math;
using CUE4Parse.UE4.Versions;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace CUE4Parse.UE4.Assets.Exports.CustomizableObject.Mutable.Mesh.Layout;

public class FLayout
{
    [JsonIgnore] public int Version = 6;
    public FIntVector2 Size;
    public FIntVector2 MaxSize;
    public EPackStrategy Strategy;
    public EReductionMethod ReductionMethod;
    public FLayoutBlock[] Blocks;
    public FImage[] Masks = [];
    
    public FLayout(FMutableArchive Ar)
    {
        if (Ar.Game >= EGame.GAME_UE5_7)
        {
            Size = Ar.Read<FIntVector2>();
            MaxSize = Ar.Read<FIntVector2>();
            Strategy = Ar.Read<EPackStrategy>();
            ReductionMethod = Ar.Read<EReductionMethod>();
            Blocks = Ar.ReadArray(() => new FLayoutBlock(Ar));
            Masks = Ar.ReadPtrArray(() => new FImage(Ar));
        }
        else if (Ar.Game >= EGame.GAME_UE5_5)
        {
            Size = Ar.Read<FIntVector2>();
            Blocks = Ar.ReadArray(() => new FLayoutBlock(Ar));
            MaxSize = Ar.Read<FIntVector2>();
            Strategy = Ar.Read<EPackStrategy>();
            ReductionMethod = Ar.Read<EReductionMethod>();
        }
        else
        {
            Version = Ar.Read<int>();
            var size = Ar.Read<TIntVector2<ushort>>();
            Size = new FIntVector2(size.X, size.Y);
            if (Version < 6)
            {
                Blocks = Ar.ReadArray(() => new FLayoutBlock(Ar, Version));
            }
            else
            {
                Blocks = Ar.ReadArray(() => new FLayoutBlock(Ar));
            }
            var maxSize = Ar.Read<TIntVector2<ushort>>();
            MaxSize = new FIntVector2(maxSize.X, maxSize.Y);
            Strategy = Ar.Read<EPackStrategy>();
            if (Version >= 4)
            {
                var FirstLODToIgnoreWarnings = Ar.Read<int>();
            }
            if (Version >= 5)
            {
                ReductionMethod = Ar.Read<EReductionMethod>();
            }
        }
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
