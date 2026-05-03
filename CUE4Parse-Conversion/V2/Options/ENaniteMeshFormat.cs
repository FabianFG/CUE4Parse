using System.ComponentModel;

namespace CUE4Parse_Conversion.V2.Options;

public enum ENaniteMeshFormat
{
    [Description("Only Nanite LOD")]
    NaniteOnly,
    [Description("Only Normal LODs")]
    NoNanite,
    [Description("Nanite LOD first, then Normal LODs")]
    NaniteFirst,
    [Description("Normal LODs first, then Nanite LOD")]
    NaniteLast,
}
