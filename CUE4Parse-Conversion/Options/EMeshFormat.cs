using System.ComponentModel;

namespace CUE4Parse_Conversion.Options;

public enum EMeshFormat
{
    [Description("ActorX (psk / pskx)")]
    ActorX,
    [Description("glTF 2.0 (binary)")]
    Gltf2,
    // 2 is free, use it
    [Description("UEFormat (uemodel)")]
    UEFormat = 3,
    [Description("Universal Scene Description (usda)")]
    USD = 4
}
