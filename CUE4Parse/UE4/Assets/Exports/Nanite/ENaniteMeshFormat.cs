using System.ComponentModel;

namespace CUE4Parse.UE4.Assets.Exports.Nanite;

public enum ENaniteMeshFormat
{
    [Description("Only Nanite LOD")]
    OnlyNaniteLOD,
    [Description("Only Normal LODs")]
    OnlyNormalLODs,
    [Description("Nanite LOD first, then Normal LODs")]
    AllLayersNaniteFirst,
    [Description("Normal LODs first, then Nanite LOD")]
    AllLayersNaniteLast,
}
