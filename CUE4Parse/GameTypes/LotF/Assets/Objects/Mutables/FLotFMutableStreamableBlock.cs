using CUE4Parse.UE4;
using CUE4Parse.UE4.Assets.Objects;
using CUE4Parse.UE4.Assets.Utils;

namespace CUE4Parse.GameTypes.LotF.Assets.Objects.Mutables;

[StructFallback]
public struct FLotFMutableStreamableBlock : IUStruct
{
    public ulong Offset;
    public ulong Size;

    public FLotFMutableStreamableBlock(FStructFallback fallback)
    {
        Offset = fallback.GetOrDefault<ulong>(nameof(Offset));
        Size = fallback.GetOrDefault<ulong>(nameof(Size));
    }
}
