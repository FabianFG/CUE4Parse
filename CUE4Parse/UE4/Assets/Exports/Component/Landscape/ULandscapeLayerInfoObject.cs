using System;
using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Objects.Core.Math;
using CUE4Parse.UE4.Objects.UObject;

namespace CUE4Parse.UE4.Assets.Exports.Component.Landscape;

public class ULandscapeLayerInfoObject : UObject {
    
    public string LayerName;
    public FLinearColor LayerUsageDebugColor;
    public FPackageIndex PhysMaterial;
    
    public override void Deserialize(FAssetArchive Ar, long validPos) {
        base.Deserialize(Ar, validPos);

        LayerName = GetOrDefault(nameof(LayerName), String.Empty);
        LayerUsageDebugColor = GetOrDefault(nameof(LayerUsageDebugColor), new FLinearColor());
        PhysMaterial = GetOrDefault<FPackageIndex>(nameof(PhysMaterial));
    }
}
