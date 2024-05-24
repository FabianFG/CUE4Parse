using CUE4Parse.UE4.Assets.Objects;
using CUE4Parse.UE4.Assets.Utils;
using CUE4Parse.UE4.Objects.Core.Math;

namespace CUE4Parse.UE4.Assets.Exports.Atom;

[StructFallback]
public readonly struct FAtomSourceModel
{
    public readonly FAtomModelPrimitive[] Primitives;
    public readonly FBox Bounds;

    public FAtomSourceModel(FStructFallback fallback)
    {
        Primitives = fallback.GetOrDefault<FAtomModelPrimitive[]>(nameof(Primitives), []);
        Bounds = fallback.GetOrDefault<FBox>(nameof(Bounds));
    }
}
