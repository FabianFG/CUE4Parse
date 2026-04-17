using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Objects.Core.Math;

namespace CUE4Parse.UE4.Assets.Exports.Component;

public class UArrowComponent : UPrimitiveComponent
{
    public FColor ArrowColor;
    public float ArrowSize;
    public float ArrowLength;

    public override void Deserialize(FAssetArchive Ar, long validPos)
    {
        base.Deserialize(Ar, validPos);

        ArrowColor = GetOrDefault(nameof(ArrowColor), new FColor(255, 0, 0));
        ArrowSize = GetOrDefault(nameof(ArrowSize), 1.0f);
        ArrowLength = GetOrDefault(nameof(ArrowLength), 80.0f);
    }
}
