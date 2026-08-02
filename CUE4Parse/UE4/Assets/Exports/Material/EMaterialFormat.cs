using System.ComponentModel;

namespace CUE4Parse.UE4.Assets.Exports.Material;

public enum EMaterialDepth
{
    [Description("Top Layer Only")]
    TopLayerOnly,
    [Description("All Layers (Without Referenced Textures)")]
    AllLayersNoRef,
    [Description("All Layers (With All Referenced Textures)")]
    AllLayers,
}
