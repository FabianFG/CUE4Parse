using System.Collections.Generic;
using CUE4Parse.UE4.Assets.Exports.Texture;

namespace CUE4Parse.UE4.Assets.Exports.Component;

public class UBillboardComponent : UPrimitiveComponent
{
    public UTexture2D? GetSprite()
    {
        var current = this;
        while (current != null)
        {
            var sprite = current.GetOrDefault<UTexture2D?>("Sprite");
            if (sprite != null) return sprite;

            current = current.Template?.Load<UBillboardComponent>();
        }

        return Owner?.Provider?.LoadPackageObject<UTexture2D>("Engine/Content/EditorResources/S_Actor.S_Actor");
    }

    public override IEnumerable<UObject> GetExportableReferences()
    {
        if (GetSprite() is { } sprite)
            yield return sprite;

        foreach (var obj in base.GetExportableReferences())
            yield return obj;
    }
}
