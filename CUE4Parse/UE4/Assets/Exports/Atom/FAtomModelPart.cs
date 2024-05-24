using CUE4Parse.UE4.Assets.Objects;
using CUE4Parse.UE4.Assets.Utils;
using CUE4Parse.UE4.Objects.Core.Math;
using CUE4Parse.UE4.Objects.UObject;

namespace CUE4Parse.UE4.Assets.Exports.Atom;

[StructFallback]
public readonly struct FAtomModelPart
{
    public readonly FSoftObjectPath AtomPrimitive;
    public readonly FSoftObjectPath MaterialInstance;
    public readonly FSoftObjectPath MaterialWithPayload;
    public readonly FTransform[] Transforms;
    public readonly uint PartId;
    public readonly string PartRevision;
    // public readonly FAtomColorSurface[] ColorSurfaces;
    // public readonly FAtomDecorationAssignment[] Decorations;
    public readonly bool bIgnoreCommonPartCulling;

    public FAtomModelPart(FStructFallback fallback)
    {
        AtomPrimitive = fallback.GetOrDefault<FSoftObjectPath>(nameof(AtomPrimitive));
        MaterialInstance = fallback.GetOrDefault<FSoftObjectPath>(nameof(MaterialInstance));
        MaterialWithPayload = fallback.GetOrDefault<FSoftObjectPath>(nameof(MaterialWithPayload));
        Transforms = fallback.GetOrDefault<FTransform[]>(nameof(Transforms));
        PartId = fallback.GetOrDefault<uint>(nameof(PartId));
        PartRevision = fallback.GetOrDefault<string>(nameof(PartRevision));
        // ColorSurfaces = fallback.GetOrDefault<FAtomColorSurface[]>(nameof(ColorSurfaces), []);
        // Decorations = fallback.GetOrDefault<FAtomDecorationAssignment[]>(nameof(Decorations), []);
        bIgnoreCommonPartCulling = fallback.GetOrDefault<bool>(nameof(bIgnoreCommonPartCulling));
    }
}
