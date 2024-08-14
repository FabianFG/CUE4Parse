using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Objects.Core.Math;

namespace CUE4Parse.UE4.Assets.Exports.Niagara;

public class UNiagaraSpriteRendererProperties : UObject
{
    public TIntVector2<float>[] BoundingGeometry = [];

    public override void Deserialize(FAssetArchive Ar, long validPos)
    {
        base.Deserialize(Ar, validPos);
        // if (!WITH_EDITORONLY_DATA || bIsCookedForEditor)
        BoundingGeometry = Ar.ReadArray<TIntVector2<float>>();
    }
}
