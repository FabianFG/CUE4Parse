using System;
using System.Diagnostics;
using CUE4Parse.UE4.Assets.Objects;
using CUE4Parse.UE4.Assets.Utils;
using CUE4Parse.UE4.Objects.UObject;

namespace CUE4Parse.UE4.Assets.Exports.Component.Landscape; 

[StructFallback]
public class FWeightmapLayerAllocationInfo: IEquatable<FWeightmapLayerAllocationInfo> {
    public readonly FPackageIndex LayerInfo;
    public readonly byte WeightmapTextureIndex;
    public readonly byte WeightmapTextureChannel;

    private string? _layerName;
    private int _layerNameHash;

    public FWeightmapLayerAllocationInfo(FStructFallback fallback) {
        LayerInfo = fallback.GetOrDefault(nameof(LayerInfo), new FPackageIndex());
        _layerName = LayerInfo.Name;
        _layerNameHash = LayerInfo.Name.GetHashCode();
        WeightmapTextureIndex = fallback.GetOrDefault<byte>(nameof(WeightmapTextureIndex));
        WeightmapTextureChannel = fallback.GetOrDefault<byte>(nameof(WeightmapTextureChannel));
    }

    public string GetLayerName() {
        if (_layerName != null) return _layerName;
        if (LayerInfo.IsNull) return "None";
        return _layerName = LayerInfo.Name;
    }

    public int GetLayerNameHash() {
        return _layerNameHash;
    }

    public bool Equals(FWeightmapLayerAllocationInfo? other) {
        if (ReferenceEquals(null, other)) return false;
        if (ReferenceEquals(this, other)) return true;
        return LayerInfo.Owner == other.LayerInfo.Owner && LayerInfo.Index == other.LayerInfo.Index &&
               WeightmapTextureIndex == other.WeightmapTextureIndex && WeightmapTextureChannel == other.WeightmapTextureChannel;
    }

    public override bool Equals(object? obj) {
        if (ReferenceEquals(null, obj)) return false;
        if (ReferenceEquals(this, obj)) return true;
        if (obj.GetType() != this.GetType()) return false;
        return Equals((FWeightmapLayerAllocationInfo)obj);
    }

    public override int GetHashCode() {
        return HashCode.Combine(LayerInfo.GetHashCode(), WeightmapTextureIndex.GetHashCode(), WeightmapTextureChannel.GetHashCode());
    }
}
