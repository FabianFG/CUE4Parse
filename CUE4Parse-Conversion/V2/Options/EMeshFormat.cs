using System.ComponentModel;

namespace CUE4Parse_Conversion.V2.Options;

public enum EMeshFormat
{
    [Description("ActorX (psk / pskx)")]
    ActorX,
    [Description("glTF 2.0 (binary)")]
    Gltf2,
    [Description("Wavefront OBJ (Not Implemented)")]
    OBJ,
    [Description("UEFormat (uemodel)")]
    UEFormat,
    [Description("Universal Scene Description (usda)")]
    USD
}
