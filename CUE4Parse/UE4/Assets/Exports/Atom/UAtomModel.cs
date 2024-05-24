using CUE4Parse.UE4.Assets.Readers;

namespace CUE4Parse.UE4.Assets.Exports.Atom;

public class UAtomModel : UObject
{
    public FAtomModelPrimitive[] Primitives;
    public FAtomSourceModel SourceModel;

    public override void Deserialize(FAssetArchive Ar, long validPos)
    {
        base.Deserialize(Ar, validPos);

        Primitives = GetOrDefault<FAtomModelPrimitive[]>(nameof(Primitives), []);
        SourceModel = GetOrDefault<FAtomSourceModel>(nameof(SourceModel));
    }
}
